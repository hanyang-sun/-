using System;
using System.IO;
using BrickDefense.Core;
using UnityEngine;

namespace BrickDefense.Infrastructure
{
    public sealed class ImportedPhoto
    {
        public ImageFrame Frame { get; }
        public byte[] OriginalBytes { get; }
        public string Extension { get; }
        internal ImportedPhoto(ImageFrame frame,byte[] bytes,string extension)
        { Frame=frame; OriginalBytes=bytes; Extension=extension; }
    }

    public static class PhotoFileSource
    {
        // The Editor installs its dialog adapter; players use the entered path.
        public static Func<string> EditorPicker { get; set; }
        public static string PickPath(string enteredPath) => EditorPicker!=null?EditorPicker():enteredPath;

        public static ImportedPhoto Load(string path)
        {
            string extension=Path.GetExtension(path).ToLowerInvariant();
            if (extension!=".png" && extension!=".jpg" && extension!=".jpeg") throw new IOException("Choose a PNG or JPG photo.");
            var info=new FileInfo(path);
            if (!info.Exists || info.Length>32*1024*1024) throw new IOException("Photo missing or larger than 32 MB.");
            byte[] bytes=File.ReadAllBytes(path);
            var decoded=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if (!ImageConversion.LoadImage(decoded,bytes,false)) throw new IOException("Could not decode this image.");
                if (decoded.width>4096 || decoded.height>4096) throw new IOException("Resize the photo to a maximum side of 4096 pixels.");
                ImageFrame frame=PhotoPixels.FromUnity("frame-"+Guid.NewGuid().ToString("N"),decoded.GetPixels32(),decoded.width,decoded.height);
                return new ImportedPhoto(frame,bytes,extension);
            }
            finally { UnityEngine.Object.Destroy(decoded); }
        }
    }
}
