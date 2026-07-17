namespace CorexProd.WPF.Helpers
{
    internal static class PdfCanvasImageCompatibilityExtensions
    {
        public static bool Image(this ProformaPdfExporter.PdfCanvas canvas, string path, double x, double y, double maxWidth, double maxHeight)
        {
            return canvas.Image(path, x, y, maxWidth, maxHeight);
        }

        public static bool Image(this ProformaPdfExporter.PdfCanvas canvas, byte[] bytes, double x, double y, double maxWidth, double maxHeight)
        {
            return canvas.Image(bytes, x, y, maxWidth, maxHeight);
        }
    }
}
