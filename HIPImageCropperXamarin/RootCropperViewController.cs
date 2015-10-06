using System;
using UIKit;
using CoreGraphics;
using System.Collections.Generic;

namespace HIPImageCropperXamarin
{
    public class RootCropperViewController : UIViewController
    {
        HIPImageCropperView _cropperView;
        UIImageView _imageView;
        List<UIButton> _photoButtons;

        public RootCropperViewController()
        {
            _photoButtons = new List<UIButton>();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            CGRect screenSize = UIScreen.MainScreen.Bounds;

            _cropperView = new HIPImageCropperView(
                frame: this.View.Bounds,
                cropSize: new CGSize(300, 300),
                position: HIPImageCropperView.CropperViewPosition.Center);

            this.View.AddSubview(_cropperView);

            _cropperView.SetOriginalImage(new UIImage("Images/portrait.jpg"));

            UIButton captureButton = new UIButton(UIButtonType.RoundedRect);
            captureButton.SetTitle("Capture", UIControlState.Normal);
            captureButton.SizeToFit();
            captureButton.Frame = new CGRect(this.View.Frame.Width - captureButton.Frame.Width - 10,
                this.View.Frame.Height - captureButton.Frame.Height - 10,
                captureButton.Frame.Width, captureButton.Frame.Height);

            captureButton.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin;
            captureButton.TouchUpInside += DidTapCaptureButton;
            this.View.AddSubview(captureButton);


            nfloat buttonSize = screenSize.Width / 3;

            for (int i = 0; i < 3; i++)
            {
                UIButton photoButton = new UIButton(UIButtonType.Custom);
                photoButton.TouchUpInside += DidTapPhotoButton;

                photoButton.Frame = new CGRect(i * buttonSize, 0, buttonSize, 50);

                string buttonTitle = string.Empty;

                switch (i)
                {
                    case 0:
                        buttonTitle = "Portrait";
                        break;
                    case 1:
                        buttonTitle = "Landscape";
                        break;
                    case 2:
                        buttonTitle = "Wide";
                        break;
                    default:
                        break;
                }

        
                photoButton.SetTitle(buttonTitle, UIControlState.Normal);

                this.View.AddSubview(photoButton);

                _photoButtons.Add(photoButton);
            }

        }

        void DidTapPhotoButton(object sender, EventArgs e)
        {
            int buttonIndex = _photoButtons.IndexOf((UIButton)sender);
            string resourceName = string.Empty;

            switch (buttonIndex)
            {
                case 0:
                    resourceName = "Images/portrait.jpg";
                    break;
                case 1:
                    resourceName = "Images/landscape.jpg";
                    break;
                case 2:
                    resourceName = "Images/landscape-wide.jpg";
                    break;
                default:
                    break;
            }

            if (string.IsNullOrEmpty(resourceName))
                return;
            
            _cropperView.SetOriginalImage(new UIImage(resourceName));
        }

        void DidTapCaptureButton(object sender, EventArgs e)
        {
            if (_imageView != null)
            {
                _imageView.RemoveFromSuperview();
                _imageView = null;
            }

            _imageView = new UIImageView(this.View.Bounds);

            _imageView.UserInteractionEnabled = true;
            _imageView.ContentMode = UIViewContentMode.Center;
            _imageView.BackgroundColor = UIColor.Black;
            _imageView.Image = _cropperView.ProcessedImage();

            this.View.AddSubview(_imageView);

            UITapGestureRecognizer tapRecognizer = new UITapGestureRecognizer(GestureRecognizerDidTap);
            _imageView.AddGestureRecognizer(tapRecognizer);
        }

        void GestureRecognizerDidTap(UIGestureRecognizer tapRecognizer)
        {
            _imageView.RemoveFromSuperview();
            _imageView = null;
        }
    }
}

