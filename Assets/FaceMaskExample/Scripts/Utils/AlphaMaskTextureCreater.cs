using UnityEngine;
using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace FaceMaskExample
{
    public class AlphaMaskTextureCreater
    {
        /// <summary>
        /// Creates an alpha mask texture.
        /// </summary>
        /// <returns>An alpha mask texture.</returns>
        /// <param name="width">The texture width.</param>
        /// <param name="height">The texture height.</param>
        /// <param name="baseArea">The base area.(An array of points in UV coordinate system)</param>
        /// <param name="exclusionAreas">Exclusion areas.(An array of points in UV coordinate system)</param>
        public static Texture2D CreateAlphaMaskTexture (float width, float height, Vector2[] baseArea, params Vector2[][] exclusionAreas)
        {
            Mat baseAreaMaskMat = new Mat ((int)height, (int)width, CvType.CV_8UC4);
            baseAreaMaskMat.setTo (new Scalar (0, 0, 0, 255));

            Mat exclusionAreaMaskMat = new Mat((int)height, (int)width, CvType.CV_8UC4);

            Point[] baseAreaPoints = new Point[baseArea.Length];
            for (int i = 0; i < baseArea.Length; i++) {
                baseAreaPoints [i] = new Point (baseArea [i].x * width, height - baseArea [i].y * height);
            }
            Imgproc.fillConvexPoly (baseAreaMaskMat, new MatOfPoint (baseAreaPoints), Scalar.all (255), Imgproc.LINE_AA, 0);

            Mat maskMat = new Mat ((int)height, (int)width, CvType.CV_8UC4);
            Core.bitwise_xor (baseAreaMaskMat, exclusionAreaMaskMat, maskMat);

            Texture2D texture = new Texture2D ((int)width, (int)height, TextureFormat.RGBA32, false);
            Utils.matToTexture2D (maskMat, texture);

            maskMat.Dispose ();
            baseAreaMaskMat.Dispose ();
            exclusionAreaMaskMat.Dispose ();
        
            return texture;
        }
    }
}