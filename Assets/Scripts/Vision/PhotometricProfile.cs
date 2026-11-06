using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Brick2Life.Vision
{
    // Locked-exposure camera response. Playback never changes these numbers and never blanks the projector.
    // camera = reflectance * (ambient + gain * command) + bias, per channel, linear light.
    public sealed class PhotometricProfile
    {
        public const double ClipLevel = 0.985;
        // Headroom for a full-white reflectance plus a full-white projector command: 0.22 + 0.62 = 0.84.
        private static readonly LinearRgb SimulationBias = new LinearRgb(0, 0, 0);
        private static readonly LinearRgb SimulationAmbient = new LinearRgb(0.22, 0.22, 0.22);
        private static readonly LinearRgb SimulationGain = new LinearRgb(0.62, 0.62, 0.62);

        public string CalibrationId { get; }
        public bool ExposureLocked { get; }
        public LinearRgb Bias { get; }
        public LinearRgb Ambient { get; }
        public LinearRgb Gain { get; }
        public LinearRgb TableReflectance { get; }
        public IReadOnlyList<LinearRgb> ColorReflectance { get; }

        private PhotometricProfile(string calibrationId, bool exposureLocked, LinearRgb bias, LinearRgb ambient,
            LinearRgb gain, LinearRgb tableReflectance, IReadOnlyList<LinearRgb> colorReflectance)
        {
            CalibrationId = calibrationId;
            ExposureLocked = exposureLocked;
            Bias = bias;
            Ambient = ambient;
            Gain = gain;
            TableReflectance = tableReflectance;
            ColorReflectance = colorReflectance;
        }

        // Synthetic stand-in used until a real camera runs Fit. The same numbers drive the composer and the recognizer.
        public static PhotometricProfile ForLockedCamera(VisionCalibration calibration)
        {
            if (calibration == null) throw new ArgumentNullException(nameof(calibration));
            var reflectance = new LinearRgb[calibration.Colors.Count];
            for (int i = 0; i < reflectance.Length; i++) reflectance[i] = LinearRgb.Decode(calibration.Colors[i].Value);
            return Create(calibration.CalibrationId, true, SimulationBias, SimulationAmbient, SimulationGain,
                LinearRgb.Decode(calibration.Background), reflectance);
        }

        public static PhotometricProfile Create(string calibrationId, bool exposureLocked, LinearRgb bias, LinearRgb ambient,
            LinearRgb gain, LinearRgb tableReflectance, IReadOnlyList<LinearRgb> colorReflectance)
        {
            if (string.IsNullOrWhiteSpace(calibrationId)) throw new ArgumentException("Calibration ID is required.", nameof(calibrationId));
            if (!exposureLocked) throw new InvalidOperationException("真实相机必须锁定曝光、增益和白平衡。");
            if (colorReflectance == null || colorReflectance.Count == 0) throw new ArgumentException("Each brick color needs a reflectance.", nameof(colorReflectance));
            return new PhotometricProfile(calibrationId, true, bias, ambient, gain, tableReflectance,
                new ReadOnlyCollection<LinearRgb>(new List<LinearRgb>(colorReflectance)));
        }

        // Setup captures, not playback. Projector black and white are shown once while the camera stays locked.
        public static PhotometricProfile Fit(string calibrationId, RgbColor darkFrame, RgbColor tableUnderBlack,
            RgbColor tableUnderWhite, RgbColor knownTableReflectance, IReadOnlyList<ColorSwatch> swatchesUnderBlack,
            bool exposureLocked)
        {
            if (!exposureLocked) throw new InvalidOperationException("真实相机必须锁定曝光、增益和白平衡后再标定。");
            if (swatchesUnderBlack == null || swatchesUnderBlack.Count == 0) throw new ArgumentException("Each brick color needs a projector-black photo.", nameof(swatchesUnderBlack));
            LinearRgb bias = LinearRgb.Decode(darkFrame);
            LinearRgb tableBlack = LinearRgb.Decode(tableUnderBlack);
            LinearRgb tableWhite = LinearRgb.Decode(tableUnderWhite);
            LinearRgb tableRho = LinearRgb.Decode(knownTableReflectance);
            var ambient = new double[3];
            var gain = new double[3];
            for (int channel = 0; channel < 3; channel++)
            {
                double rho = Channel(tableRho, channel);
                if (rho < 0.02) throw new InvalidOperationException("灰卡反射率过低，无法分开环境光和投影增益。");
                double signal = Channel(tableBlack, channel) - Channel(bias, channel);
                double added = Channel(tableWhite, channel) - Channel(tableBlack, channel);
                if (signal <= 1e-4) throw new InvalidOperationException("全黑投影下桌面几乎没有信号，请提高环境光或曝光。");
                if (added < 0) throw new InvalidOperationException("全白投影没有比全黑更亮，请确认投影机确实点亮且曝光未改变。");
                ambient[channel] = signal / rho;
                gain[channel] = added / rho;
                if (ambient[channel] < 0 || gain[channel] < 0 || ambient[channel] > 4 || gain[channel] > 4)
                    throw new InvalidOperationException("标定得到的光照超出可解释范围，请重新拍摄且避免爆光。");
            }
            var reflectance = new LinearRgb[swatchesUnderBlack.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < swatchesUnderBlack.Count; i++)
            {
                ColorSwatch swatch = swatchesUnderBlack[i];
                if (swatch == null || !seen.Add(swatch.Id)) throw new ArgumentException("标定颜色必须唯一。", nameof(swatchesUnderBlack));
                LinearRgb photo = LinearRgb.Decode(swatch.Value);
                reflectance[i] = new LinearRgb(
                    ClampUnit((photo.R - bias.R) / ambient[0]),
                    ClampUnit((photo.G - bias.G) / ambient[1]),
                    ClampUnit((photo.B - bias.B) / ambient[2]));
            }
            return Create(calibrationId, true, bias, new LinearRgb(ambient[0], ambient[1], ambient[2]),
                new LinearRgb(gain[0], gain[1], gain[2]), tableRho, reflectance);
        }

        public LinearRgb Predict(LinearRgb reflectance, double commandRed, double commandGreen, double commandBlue, out bool clipped)
        {
            double illumRed, illumGreen, illumBlue;
            Illumination(commandRed, commandGreen, commandBlue, out illumRed, out illumGreen, out illumBlue);
            double r = Bias.R + reflectance.R * illumRed;
            double g = Bias.G + reflectance.G * illumGreen;
            double b = Bias.B + reflectance.B * illumBlue;
            clipped = r > ClipLevel || g > ClipLevel || b > ClipLevel;
            return new LinearRgb(ClampUnit(r), ClampUnit(g), ClampUnit(b));
        }

        // False when the pixel is saturated or the projector command cannot explain it. Callers must not guess a color.
        public bool TryRecover(LinearRgb camera, double commandRed, double commandGreen, double commandBlue, out LinearRgb reflectance)
        {
            reflectance = default;
            if (camera.R >= ClipLevel || camera.G >= ClipLevel || camera.B >= ClipLevel) return false;
            double illumRed, illumGreen, illumBlue;
            Illumination(commandRed, commandGreen, commandBlue, out illumRed, out illumGreen, out illumBlue);
            if (illumRed < 1e-3 || illumGreen < 1e-3 || illumBlue < 1e-3) return false;
            double r = (camera.R - Bias.R) / illumRed;
            double g = (camera.G - Bias.G) / illumGreen;
            double b = (camera.B - Bias.B) / illumBlue;
            if (r < -0.02 || g < -0.02 || b < -0.02 || r > 1.05 || g > 1.05 || b > 1.05) return false;
            reflectance = new LinearRgb(ClampUnit(r), ClampUnit(g), ClampUnit(b));
            return true;
        }

        private void Illumination(double commandRed, double commandGreen, double commandBlue,
            out double red, out double green, out double blue)
        {
            red = Ambient.R + Gain.R * commandRed;
            green = Ambient.G + Gain.G * commandGreen;
            blue = Ambient.B + Gain.B * commandBlue;
            if (red < 0 || green < 0 || blue < 0) throw new InvalidOperationException("光照不能为负。");
        }

        private static double Channel(LinearRgb color, int channel)
        {
            if (channel == 0) return color.R;
            if (channel == 1) return color.G;
            return color.B;
        }

        private static double ClampUnit(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("光照计算出现非有限数。");
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }
    }
}
