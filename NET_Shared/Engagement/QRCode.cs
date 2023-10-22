using QRCoder;

namespace qrcode
{
    public static class QRCode
    {
        public static byte[] Generate(string data)
        {
            var gen = new QRCodeGenerator();
            // Creates Qr code with data to be sent
            QRCodeData qrdata = gen.CreateQrCode("mdoc:" + data, QRCodeGenerator.ECCLevel.Q);
            BitmapByteQRCode code = new BitmapByteQRCode(qrdata);
            byte[] imageBytes = code.GetGraphic(20);

            return imageBytes;
        }
    }
}