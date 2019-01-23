//using System;
//using Size = System.Drawing.Size;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using PanoBeamLib;

//namespace PanoBeamTests
//{
//    [TestClass]
//    public class UnitTest1
//    {
//        [TestMethod]
//        public void TestCalibrate()
//        {
//            var patternSize = 50;
//            var patternCount = new Size(3,3);
//            var screen = new Screen
//            {
//                Resolution = new Size(3240, 1080),
//                Overlap = 600
//            };
//            screen.AddProjectors();
//            screen.SetPattern(patternSize, patternCount, false, true);
//            screen.Threshold = 20;
//            //screen.InitializeControlPoints();
//            //var rect = CameraUserControl.GetClippingRectangle();
//            //screen.ClippingRectangle = rect.GetRectangle();
//            screen.AwaitProjectorsReady = AwaitProjectorsReady;
//            screen.Calibrate();
//        }

//        private void AwaitProjectorsReady(Action continueAction, Action calibrationCanceled, CalibrationSteps[] calibrationSteps)
//        {
//            continueAction();
//        }
//    }
//}
