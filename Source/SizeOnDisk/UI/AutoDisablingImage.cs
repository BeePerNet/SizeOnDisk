using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SizeOnDisk.UI
{
    public class AutoDisablingImage : Image
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoDisablingImage"/> class.
        /// </summary>

        static AutoDisablingImage()
        {
            // Override the metadata of the IsEnabled property.
            IsEnabledProperty.OverrideMetadata(typeof(AutoDisablingImage), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnAutoGreyScaleImageIsEnabledPropertyChanged)));
        }

        /// <summary>
        /// Called when [auto grey scale image is enabled property changed].
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnAutoGreyScaleImageIsEnabledPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            if (!args.OldValue.Equals(args.NewValue))
            {
                AutoDisablingImage autoGreyScaleImg = source as AutoDisablingImage;
                if (autoGreyScaleImg != null)
                {
                    if (!(args.NewValue as bool? ?? false))
                    {
                        // Get the source bitmap
                        var bitmapImage = new BitmapImage(new Uri(autoGreyScaleImg.Source.ToString()));
                        // Convert it to Gray
                        autoGreyScaleImg.Source = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray32Float, null, 0);
                        // Create Opacity Mask for greyscale image as FormatConvertedBitmap does not keep transparency info
                        autoGreyScaleImg.OpacityMask = new ImageBrush(bitmapImage);
                        autoGreyScaleImg.Opacity = 0.5;
                    }
                    else
                    {
                        // Set the Source property to the original value.
                        autoGreyScaleImg.Source = ((FormatConvertedBitmap)autoGreyScaleImg.Source).Source;
                        // Reset the Opcity Mask
                        autoGreyScaleImg.OpacityMask = null;
                        autoGreyScaleImg.Opacity = 1;
                    }
                }
            }
        }


    }
}
