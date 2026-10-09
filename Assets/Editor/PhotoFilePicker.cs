using BrickDefense.Infrastructure;
using UnityEditor;

namespace BrickDefense.Editor
{
    [InitializeOnLoad]
    internal static class PhotoFilePicker
    {
        static PhotoFilePicker() { PhotoFileSource.EditorPicker=() => EditorUtility.OpenFilePanel("Select board photograph","",""); }
    }
}
