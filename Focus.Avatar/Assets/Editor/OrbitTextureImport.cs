using UnityEditor;

namespace Focus.Avatar.Editor
{
    public sealed class OrbitTextureImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath != "Assets/Resources/OrbitShell.png")
                return;

            var texture = (TextureImporter)assetImporter;
            texture.isReadable = true;
            texture.alphaIsTransparency = true;
            texture.mipmapEnabled = false;
            texture.maxTextureSize = 512;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
