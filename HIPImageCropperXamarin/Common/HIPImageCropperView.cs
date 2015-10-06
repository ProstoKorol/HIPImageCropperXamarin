using UIKit;
using System;
using CoreGraphics;

//
//  Original of HIPImageCropperView created by Taylan Pince on 2013-05-27.
//
//
//  Ported to Xamarin iOS by Alexandr Korolev on 2015-07-20
//

//based on https://github.com/Hipo/HIPImageCropper/blob/master/Dependencies/HIPImageCropperView/HIPImageCropperView.m
using Foundation;
using CoreAnimation;


namespace HIPImageCropperXamarin
{
    public sealed class HIPImageCropperView : UIView
    {
        #region fields

        bool _borderVisible;
        CropperViewPosition _maskPosition;
        CropHoleType _cropHoleType;
        CGSize _cropSize;
        CGSize _targetSize;
        nfloat _cropSizeRatio;
        UIScrollView _scrollView;
        UIImage _originalImage;

        #endregion

        #region properties

        UIScrollView ScrollView { get { return _scrollView; } set { _scrollView = value; } }

        UIImageView ImageView { get; set; }

        UIView OverlayView { get; set; }

        UIActivityIndicatorView LoadIndicator { get; set; }

        nfloat CropSizeRatio { get; set; }

        CGSize TargetSize { get; set; }

        CropperViewPosition MaskPosition { get; set; }


        public bool BorderVisible
        {
            get { return _borderVisible; }
            set
            {
                _borderVisible = value;
                UpdateOverlay();
            }
        }

        public nfloat ZoomScale
        {
            get{ return ScrollView.ZoomScale; }
        }

        #endregion

        #region override properties

        public override CGRect Frame
        {
            get
            {
                return base.Frame;
            }
            set
            {
                base.Frame = value;
                UpdateOverlay();
            }
        }

        #endregion

        public HIPImageCropperView(CGRect frame, CGSize cropSize, CropperViewPosition position, CropHoleType cropHoleType = CropHoleType.Square)
            : base(frame)
        {
            _maskPosition = position;
            _cropHoleType = cropHoleType;
            _cropSize = cropSize;
            this.BackgroundColor = UIColor.Black;
            this.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            this.ClipsToBounds = true;

            ScrollView = new UIScrollView();

            ImageView = new UIImageView();

            OverlayView = new UIView()
            {
                UserInteractionEnabled = false,
                BackgroundColor = UIColor.Black.ColorWithAlpha(0.6f),
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
            };
            
            LoadIndicator = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.White)
            {
                HidesWhenStopped = true,
                AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin |
                UIViewAutoresizing.FlexibleRightMargin |
                UIViewAutoresizing.FlexibleBottomMargin |
                UIViewAutoresizing.FlexibleTopMargin
            };
            
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            nfloat defaultInset = 1;

            CGSize maxSize = new CGSize(this.Bounds.Size.Width - (defaultInset * 2),
                                 this.Bounds.Size.Height - (defaultInset * 2));
            _borderVisible = true;
            _targetSize = _cropSize;
            _cropSizeRatio = 1;

            if (_cropSize.Width >= _cropSize.Height)
            {
                if (_cropSize.Width > maxSize.Width)
                {
                    _cropSizeRatio = _cropSize.Width / maxSize.Width;
                }
            }
            else
            {
                if (_cropSize.Height > maxSize.Height)
                {
                    _cropSizeRatio = _cropSize.Height / maxSize.Height;
                }
            }

            _cropSize = new CGSize(_cropSize.Width / _cropSizeRatio, _cropSize.Height / _cropSizeRatio);

            nfloat scrollViewVerticalPosition = 0;

            switch (_maskPosition)
            {
                case CropperViewPosition.Bottom:
                    scrollViewVerticalPosition = rect.Size.Height - _cropSize.Height;
                    break;
                case CropperViewPosition.Center:
                    scrollViewVerticalPosition = (rect.Size.Height - _cropSize.Height) / 2;
                    break;
                case CropperViewPosition.Top:
                    scrollViewVerticalPosition = 0;
                    break;
            }

            ScrollView.Frame = new CGRect((this.Bounds.Size.Width - _cropSize.Width) / 2,
                scrollViewVerticalPosition, _cropSize.Width, _cropSize.Height);
            ScrollView.ViewForZoomingInScrollView += (UIScrollView sv) => ImageView;
            ScrollView.Bounces = true;
            ScrollView.BouncesZoom = true;
            ScrollView.AlwaysBounceVertical = true;
            ScrollView.AlwaysBounceHorizontal = true;
            ScrollView.ShowsVerticalScrollIndicator = false;
            ScrollView.ShowsHorizontalScrollIndicator = false;
            ScrollView.Layer.MasksToBounds = false;
            ScrollView.BackgroundColor = UIColor.Black.ColorWithAlpha(0.8f);

            UITapGestureRecognizer doubleTapRecognizer = new UITapGestureRecognizer(DidTriggerDoubleTapGesture);
            doubleTapRecognizer.NumberOfTapsRequired = 2;
            doubleTapRecognizer.NumberOfTouchesRequired = 1;
            ScrollView.AddGestureRecognizer(doubleTapRecognizer);

            switch (_maskPosition)
            {
                case CropperViewPosition.Bottom:
                    ScrollView.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin |
                    UIViewAutoresizing.FlexibleRightMargin |
                    UIViewAutoresizing.FlexibleTopMargin;
                    break;
                case CropperViewPosition.Center:
                    ScrollView.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin |
                    UIViewAutoresizing.FlexibleRightMargin |
                    UIViewAutoresizing.FlexibleTopMargin |
                    UIViewAutoresizing.FlexibleBottomMargin;
                    break;
                case CropperViewPosition.Top:
                    ScrollView.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin |
                    UIViewAutoresizing.FlexibleRightMargin |
                    UIViewAutoresizing.FlexibleBottomMargin;
                    break;
            }

            this.AddSubview(ScrollView);

            ImageView.Frame = ScrollView.Bounds;
            ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;

            ImageView.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin |
            UIViewAutoresizing.FlexibleLeftMargin |
            UIViewAutoresizing.FlexibleRightMargin |
            UIViewAutoresizing.FlexibleBottomMargin;

            ScrollView.AddSubview(ImageView);

            OverlayView.Frame = this.Bounds;
            this.AddSubview(OverlayView);

            UpdateOverlay();

           
            LoadIndicator.Center = ScrollView.Center;
            this.AddSubview(LoadIndicator);
        }

       
        void StartLoadingAnimated(bool animated)
        {
            LoadIndicator.StartAnimating();
        
            UIView.Animate((animated) ? 0.2 : 0.0,
                () =>
                {
                    ImageView.Alpha = 0f;
                    LoadIndicator.Alpha = 1f;
                });
        }

        public void SetOriginalImage(UIImage originalImage)
        {
            SetOriginalImage(originalImage, CGRect.Empty);
        }

        public void SetOriginalImage(UIImage originalImage, CGRect cropFrame)
        {
            LoadIndicator.StartAnimating();
            InvokeOnMainThread(() =>
                {
                    CGImage imageRef = originalImage.CGImage;
                    UIImageOrientation imageOrientation = originalImage.Orientation;
                    
                    if (imageRef == null)
                        return;
                    
                    
                    var bytesPerRow = 0;
                    var width = imageRef.Width;
                    var height = imageRef.Height;
                    var bitsPerComponent = imageRef.BitsPerComponent;
                    CGColorSpace colorSpace = CGColorSpace.CreateDeviceRGB();
                    CGBitmapFlags bitmapInfo = imageRef.BitmapInfo;
                    
                    switch (imageOrientation)
                    {
                        case UIImageOrientation.RightMirrored:
                        case UIImageOrientation.LeftMirrored:
                        case UIImageOrientation.Right:
                        case UIImageOrientation.Left:
                            width = imageRef.Height;
                            height = imageRef.Width;
                            break;
                        default:
                            break;
                    }
                    
                    CGSize imageSize = new CGSize(width, height);
                    CGBitmapContext context = new CGBitmapContext(null,
                                                  width,
                                                  height,
                                                  bitsPerComponent,
                                                  bytesPerRow,
                                                  colorSpace,
                                                  bitmapInfo);
                    
                    colorSpace.Dispose();
                    
                    if (context == null)
                    {
                        imageRef.Dispose();
                        return;
                    }
                    
                    switch (imageOrientation)
                    {
                        case UIImageOrientation.RightMirrored:
                        case UIImageOrientation.Right:
                            context.TranslateCTM(imageSize.Width / 2, imageSize.Height / 2);
                            context.RotateCTM(-((nfloat)Math.PI / 2));
                            context.TranslateCTM(-imageSize.Height / 2, -imageSize.Width / 2);
                            break;
                        case UIImageOrientation.LeftMirrored:
                        case UIImageOrientation.Left:
                            context.TranslateCTM(imageSize.Width / 2, imageSize.Height / 2);
                            context.RotateCTM((nfloat)(Math.PI / 2));
                            context.TranslateCTM(-imageSize.Height / 2, -imageSize.Width / 2);
                            break;
                        case UIImageOrientation.Down:
                        case UIImageOrientation.DownMirrored:
                            context.TranslateCTM(imageSize.Width / 2, imageSize.Height / 2);
                            context.RotateCTM((nfloat)Math.PI);
                            context.TranslateCTM(-imageSize.Width / 2, -imageSize.Height / 2);
                            break;
                        default:
                            break;
                    }
                    
                    context.InterpolationQuality = CGInterpolationQuality.High;
                    context.SetBlendMode(CGBlendMode.Copy);
                    context.DrawImage(new CGRect(0, 0, imageRef.Width, imageRef.Height), imageRef);
                    
                    CGImage contextImage = context.ToImage();
                    
                    context.Dispose();
                    
                    if (contextImage != null)
                    {
                        _originalImage = UIImage.FromImage(contextImage, originalImage.CurrentScale, UIImageOrientation.Up);
                    
                        contextImage.Dispose();
                    }
                    
                    imageRef.Dispose();
                   
                    BeginInvokeOnMainThread(() =>
                        {
                            CGSize convertedImageSize = new CGSize(_originalImage.Size.Width / _cropSizeRatio,
                                                            _originalImage.Size.Height / _cropSizeRatio);
                            
                            ImageView.Alpha = 0;
                            ImageView.Image = _originalImage;
                            
                            CGSize sampleImageSize = new CGSize(Math.Max(convertedImageSize.Width, ScrollView.Frame.Size.Width),
                                                         Math.Max(convertedImageSize.Height, ScrollView.Frame.Size.Height));
                            
                            ScrollView.MinimumZoomScale = 1;
                            ScrollView.MaximumZoomScale = 1;
                            ScrollView.ZoomScale = 1;
                            ImageView.Frame = new CGRect(0, 0, convertedImageSize.Width, convertedImageSize.Height);
                            
                            nfloat zoomScale = 1;
                            
                            if (convertedImageSize.Width < convertedImageSize.Height)
                            {
                                zoomScale = (ScrollView.Frame.Size.Width / convertedImageSize.Width);
                            }
                            else
                            {
                                zoomScale = (ScrollView.Frame.Size.Height / convertedImageSize.Height);
                            }
                            
                            ScrollView.ContentSize = sampleImageSize;
                            
                            if (zoomScale < 1)
                            {
                                ScrollView.MinimumZoomScale = zoomScale;
                                ScrollView.MaximumZoomScale = 1;
                                ScrollView.ZoomScale = zoomScale;
                            }
                            else
                            {
                                ScrollView.MinimumZoomScale = zoomScale;
                                ScrollView.MaximumZoomScale = zoomScale;
                                ScrollView.ZoomScale = zoomScale;
                            }
                            
                            ScrollView.ContentInset = UIEdgeInsets.Zero;
                            ScrollView.ContentOffset = new CGPoint((ImageView.Frame.Size.Width - ScrollView.Frame.Size.Width) / 2, (ImageView.Frame.Size.Height - ScrollView.Frame.Size.Height) / 2);
                            if (cropFrame.Size.Width > 0 && cropFrame.Size.Height > 0)
                            {
                                nfloat scale = UIScreen.MainScreen.Scale;
                                nfloat newZoomScale = (_targetSize.Width * scale) / cropFrame.Size.Width;
                            
                                ScrollView.ZoomScale = newZoomScale;
                            
                                nfloat heightAdjustment = (_targetSize.Height / _cropSizeRatio) - ScrollView.ContentSize.Height;
                                nfloat offsetY = cropFrame.Y + (heightAdjustment * _cropSizeRatio * scale);
                            
                                ScrollView.ContentOffset = new CGPoint(cropFrame.X / scale / _cropSizeRatio,
                                    (offsetY / scale / _cropSizeRatio) - heightAdjustment);
                            }
                            
                            ScrollView.SetNeedsLayout();
                            
                            UIView.Animate(0.3,
                                () =>
                                {
                                    LoadIndicator.Alpha = 0;
                                    ImageView.Alpha = 1;
                                }, () =>
                                {
                                    LoadIndicator.StopAnimating();
                                });
                        });
                });
        }


        public void SetScrollViewTopOffset(nfloat scrollViewTopOffset)
        {
            CGRect scrollViewFrame = _scrollView.Frame;
            scrollViewFrame.Y += scrollViewTopOffset;
            _scrollView.Frame = scrollViewFrame;
        
            LoadIndicator.Center = ScrollView.Center;
        
            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            if (OverlayView == null)
                return;
            foreach (var subview in this.OverlayView.Subviews)
            {
                subview.RemoveFromSuperview();
            }
            
            if (_borderVisible)
            {
                UIView borderView = new UIView(ScrollView.Frame.Inset(-1.0f, -1.0f));
            
                borderView.Layer.BorderColor = UIColor.White.ColorWithAlpha(0.5f).CGColor;
                borderView.Layer.BorderWidth = 1.0f;
                borderView.BackgroundColor = UIColor.Clear;
                borderView.AutoresizingMask = ScrollView.AutoresizingMask;
                if (_cropHoleType == CropHoleType.Circle)
                    borderView.Layer.CornerRadius = borderView.Frame.Height / 2;
                OverlayView.AddSubview(borderView);
            }
            
            CAShapeLayer maskWithHole = new CAShapeLayer();
            
            CGRect biggerRect = OverlayView.Bounds;
            CGRect smallerRect = ScrollView.Frame;
            
            UIBezierPath maskPath = new UIBezierPath();
            
            maskPath.MoveTo(new CGPoint(biggerRect.GetMinX(), biggerRect.GetMinY()));
            maskPath.AddLineTo(new CGPoint(biggerRect.GetMinX(), biggerRect.GetMaxY()));
            maskPath.AddLineTo(new CGPoint(biggerRect.GetMaxX(), biggerRect.GetMaxY()));
            maskPath.AddLineTo(new CGPoint(biggerRect.GetMaxX(), biggerRect.GetMinY()));
            maskPath.AddLineTo(new CGPoint(biggerRect.GetMinX(), biggerRect.GetMinY()));

            switch (_cropHoleType)
            {
                case CropHoleType.Square:
                    {
                        maskPath.MoveTo(new CGPoint(smallerRect.GetMinX(), smallerRect.GetMinY()));
                        maskPath.AddLineTo(new CGPoint(smallerRect.GetMinX(), smallerRect.GetMaxY()));
                        maskPath.AddLineTo(new CGPoint(smallerRect.GetMaxX(), smallerRect.GetMaxY()));
                        maskPath.AddLineTo(new CGPoint(smallerRect.GetMaxX(), smallerRect.GetMinY()));
                        maskPath.AddLineTo(new CGPoint(smallerRect.GetMinX(), smallerRect.GetMinY()));
                    }
                    break;
                case CropHoleType.Circle:
                    {
                        maskPath.MoveTo(new CGPoint(smallerRect.GetMaxX(), smallerRect.GetMidY()));
                        maskPath.AddArc(
                            center: ScrollView.Center,
                            radius: smallerRect.Height / 2,
                            startAngle: DegreesToRadians(0),
                            endAngle: DegreesToRadians(360),
                            clockWise: true);
                    }
                    break;
            }

            maskWithHole.Frame = this.Bounds;
            maskWithHole.Path = maskPath.CGPath;
            maskWithHole.FillRule = CAShapeLayer.FillRuleEvenOdd;

            OverlayView.Layer.Mask = maskWithHole;
        }

        public UIImage ProcessedImage()
        {
            nfloat scale = UIScreen.MainScreen.Scale;
            CGImage imageRef = _originalImage.CGImage;
            
            if (imageRef == null)
                return null;
            
            
            var bytesPerRow = 0;
            var bitsPerComponent = imageRef.BitsPerComponent;
            CGColorSpace colorSpace = CGColorSpace.CreateDeviceRGB();
            var bitmapInfo = imageRef.BitmapInfo;
            CGBitmapContext context = new CGBitmapContext(null,
                                          (nint)(_targetSize.Width * scale),
                                          (nint)(_targetSize.Height * scale),
                                          bitsPerComponent,
                                          bytesPerRow,
                                          colorSpace,
                                          bitmapInfo);
           
            colorSpace.Dispose();
            if (context == null)
            {
                imageRef.Dispose();
                return null;
            }
            
            CGRect targetFrame = LocalCropFrame();

            context.InterpolationQuality = CGInterpolationQuality.High;
            context.SetBlendMode(CGBlendMode.Copy);
            context.DrawImage(targetFrame, imageRef);

            var contextImage = context.ToImage();
            UIImage finalImage = null;

            context.Dispose();

            if (contextImage != null)
            {
                finalImage = UIImage.FromImage(contextImage, scale, UIImageOrientation.Up);
                contextImage.Dispose();
            }
            
            imageRef.Dispose();
            return finalImage;
        }

        CGRect LocalCropFrame()
        {
            nfloat scale = UIScreen.MainScreen.Scale;
            CGSize originalImageSize = _originalImage.Size;
            nfloat actualHeight = originalImageSize.Height * ScrollView.ZoomScale * scale;
            nfloat actualWidth = originalImageSize.Width * ScrollView.ZoomScale * scale;
            nfloat heightAdjustment = (_targetSize.Height / _cropSizeRatio) - ScrollView.ContentSize.Height;
            nfloat offsetX = -(ScrollView.ContentOffset.X * _cropSizeRatio * scale);
            nfloat offsetY = (ScrollView.ContentOffset.Y + heightAdjustment) * _cropSizeRatio * scale;
            CGRect targetFrame = new CGRect(offsetX, offsetY, actualWidth, actualHeight);

            return targetFrame;
        }

        public CGRect CropFrame()
        {
            CGRect localCropFrame = LocalCropFrame();
            nfloat scale = UIScreen.MainScreen.Scale;
            nfloat heightAdjustment = (_targetSize.Height / _cropSizeRatio) - ScrollView.ContentSize.Height;
        
            return new CGRect(-localCropFrame.X, localCropFrame.Y - (heightAdjustment * _cropSizeRatio * scale),
                _targetSize.Width / ScrollView.ZoomScale * scale,
                _targetSize.Height / ScrollView.ZoomScale * scale);
        }

        #region Gesture recognizers

        void DidTriggerDoubleTapGesture(UITapGestureRecognizer tapRecognizer)
        {
            nfloat currentZoomScale = ScrollView.ZoomScale;
            nfloat maxZoomScale = ScrollView.MaximumZoomScale;
            nfloat minZoomScale = ScrollView.MinimumZoomScale;
            nfloat zoomRange = maxZoomScale - minZoomScale;

            if (zoomRange <= 0.0)
            {
                return;
            }

            nfloat zoomPosition = (currentZoomScale - minZoomScale) / zoomRange;

            if (zoomPosition <= 0.5)
                ScrollView.SetZoomScale(maxZoomScale, true);
            else
                ScrollView.SetZoomScale(minZoomScale, true);
        }

        #endregion

        public nfloat DegreesToRadians(float degrees)
        {
            return (nfloat)((Math.PI * degrees) / 180);
        }

        public enum CropperViewPosition
        {
            Top,
            Center,
            Bottom,
        }

        public enum CropHoleType
        {
            Square,
            Circle,
        }
    }
}