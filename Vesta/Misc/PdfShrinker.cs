using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vesta.MVVM;

namespace Vesta.Misc
{
    enum PdfShrinkStatus
    {
        Initialising,
        Resampling,
        Failed
    }


    class PdfShrinker : ObservableObject
    {
        private FileInfo _OriginalFile;
        private ShrinkOptions _ShrinkOptions;

        private DateTime _OriginalAccess;
        private DateTime _OriginalCreation;
        private DateTime _OriginalModified;
        private FileAttributes _OriginalAttributes;

        public long OriginalSize { get; private set; }
        public long NewSize { get; private set; }
        public string OriginalFullName { get; private set; }
        public string OriginalName { get; private set; }
        public string NewFullName { get; private set; }
        public string NewName { get; private set; }

        public PdfShrinker(FileInfo fileToShrink, ShrinkOptions options)
        {
            ChangeStatus(PdfShrinkStatus.Initialising, 0);
            _OriginalFile = fileToShrink;
            _ShrinkOptions = options;

            _OriginalAccess = fileToShrink.LastAccessTime;
            _OriginalCreation = fileToShrink.CreationTime;
            _OriginalModified = fileToShrink.LastWriteTime;
            _OriginalAttributes = fileToShrink.Attributes;

            OriginalSize = fileToShrink.Length;
            OriginalFullName = fileToShrink.FullName;
            OriginalName = fileToShrink.Name;
        }

        #region Observable Properties

        private PdfShrinkStatus _Status;

        public PdfShrinkStatus Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
                RaisePropertyChangedEvent("Status");
                RaisePropertyChangedEvent("StatusString");
            }
        }

        private double _Percent;

        public double Percent
        {
            get { return _Percent; }
            set
            {
                _Percent = value;
                RaisePropertyChangedEvent("Percent");
                RaisePropertyChangedEvent("StatusString");
            }
        }

        public string StatusString
        {
            get
            {
                string result = System.Enum.GetName(typeof(PdfShrinkStatus), Status);
                if (Percent != 0)
                {
                    result += " (" + Percent.ToString("##%") +")";
                }
                return result;
            }
        }

        #endregion

        #region Private Methods

        private void ChangeStatus(PdfShrinkStatus newStatus)
        {
            ChangeStatus(newStatus, 0);
        }

        private void ChangeStatus(PdfShrinkStatus newStatus, double percent)
        {
            Status = newStatus;
            Percent = percent;
        }


        private void ShrinkPdfPage(PdfPage page, int quality)
        {
            PdfDictionary resources = page.Elements.GetDictionary("/Resources");
            if (resources != null)
            {
                PdfDictionary xObjects = resources.Elements.GetDictionary("/XObject");
                if (xObjects != null)
                {
                    ICollection<PdfItem> items = xObjects.Elements.Values;
                    foreach (PdfItem item in items)
                    {
                        PdfReference reference = item as PdfReference;
                        if (reference != null)
                        {
                            PdfDictionary xObject = reference.Value as PdfDictionary;
                            if (xObject != null && xObject.Elements.GetString("/Subtype") == "/Image")
                            {
                                ShrinkJpegImage(xObject, quality);
                            }
                        }
                    }
                }
            }
        }

        private void ShrinkJpegImage(PdfDictionary image, int quality)
        {
            try
            {
                string filter = image.Elements.GetName("/Filter");
                if (filter != "/DCTDecode") return;

                Stream originalImageStream = new MemoryStream(image.Stream.Value);
                Image originalJpegImage = Image.FromStream(originalImageStream);

                byte[] newJpegImage = SaveJpeg(originalJpegImage, quality);

                image.Stream.Value = newJpegImage;
            }
            catch (Exception)
            {
                return;
            }
        }

        private byte[] SaveJpeg(System.Drawing.Image img, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            MemoryStream outputStream = new MemoryStream();
            img.Save(outputStream, jpegCodec, encoderParams);
            byte[] outputBuffer = outputStream.ToArray();

            return outputBuffer;
        }


        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        } 


        #endregion

        #region Public Methods

        public void ShrinkPdf()
        {
            PdfDocument document;

            try
            {
                document = CompatiblePdfReader.Open
                    (new MemoryStream(File.ReadAllBytes(_OriginalFile.FullName)),
                    PdfDocumentOpenMode.Modify);
            }
            catch (Exception)
            {
                ChangeStatus(PdfShrinkStatus.Failed);
                return;
            }

            int pageCount = 1;

            foreach (PdfPage page in document.Pages)
            {
                double currentProgress = pageCount / (double)document.PageCount;
                ChangeStatus(PdfShrinkStatus.Resampling, currentProgress);
                ShrinkPdfPage(page, _ShrinkOptions.EncodingQuality);
            }

            if (_ShrinkOptions.SaveOption == SaveOption.Overwrite)
            {
                document.Save(_OriginalFile.FullName);
                document.Close();
                if (_ShrinkOptions.RetainAccessedDate)
                    File.SetLastAccessTime(_OriginalFile.FullName, _OriginalAccess);
                if (_ShrinkOptions.RetainCreationDate)
                    File.SetCreationTime(_OriginalFile.FullName, _OriginalCreation);
                if (_ShrinkOptions.RetainModifiedDate)
                    File.SetLastWriteTime(_OriginalFile.FullName, _OriginalModified);
                if (_ShrinkOptions.RetainAttributes)
                    File.SetAttributes(_OriginalFile.FullName, _OriginalAttributes);

                NewFullName = _OriginalFile.FullName;
                NewName = _OriginalFile.Name;
                FileInfo newFile = new FileInfo(_OriginalFile.FullName);
                NewSize = newFile.Length;
            }

        }

        #endregion

        public override string ToString()
        {
            //TODO: need to make this output the results into a CSV format
            //so that it can display report on screen.
            return base.ToString();
        }

    }
}
