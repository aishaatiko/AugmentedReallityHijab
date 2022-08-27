using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DlibFaceLandmarkDetector;
using OpenCVForUnity.RectangleTrack;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.ImgprocModule;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace FaceMaskExample
{
    /// <summary>
    /// WebCamTexture FaceMask Additional Example
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper), typeof(TrackedMeshOverlay))]
    public class WebCamTextureFaceMaskAdditionalExample : MonoBehaviour
    {
        //tampilan di unity editor
        [HeaderAttribute ("Additional FaceMask Option")]
      
        /// <summary>
        /// Determines add Hijab points.
        /// </summary>
        public bool extendHijab = true;

        [Space (15)]

        [HeaderAttribute ("FaceMaskData")]

        /// <summary>
        /// The face mask data list.
        /// </summary>
        public List<FaceMaskData> faceMaskDatas;

        [HeaderAttribute ("Option")]
      
        /// <summary>
        /// Determines if enables color correction.
        /// </summary>
        public bool enableColorCorrection = true;

        /// <summary>
        /// The enable color correction toggle.
        /// </summary>
        public Toggle enableColorCorrectionToggle;
               
        /// <summary>
        /// Determines if displays face rects.
        /// </summary>
        public bool displayFaceRects = false;
        
        /// <summary>
        /// The toggle for switching face rects display state
        /// </summary>
        public Toggle displayFaceRectsToggle;

        /// <summary>
        /// Determines if displays debug face points.
        /// </summary>
        public bool displayDebugFacePoints = false;

        /// <summary>
        /// The toggle for switching debug face points display state.
        /// </summary>
        public Toggle displayDebugFacePointsToggle;
        
        //batas tampilan di unity editor

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;
        
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;
        
        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The detection based tracker.
        /// </summary>
        RectangleTracker rectangleTracker;

        /// <summary>
        /// The web cam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;
        
        /// <summary>
        /// The face landmark detector.
        /// </summary>
        FaceLandmarkDetector faceLandmarkDetector;

        /// <summary>
        /// The mean points filter dictionary.
        /// </summary>
        Dictionary<int, LowPassPointsFilter> lowPassFilterDict;

        /// <summary>
        /// The optical flow points filter dictionary.
        /// </summary>
        Dictionary<int, OFPointsFilter> opticalFlowFilterDict;

        /// <summary>
        /// The face mask color corrector.
        /// </summary>
        FaceMaskColorCorrector faceMaskColorCorrector;
             
        /// <summary>
        /// The mesh overlay.
        /// </summary>
        TrackedMeshOverlay meshOverlay;

        /// <summary>
        /// The Shader.PropertyToID for "_Fade".
        /// </summary>
        int shader_FadeID;

        /// <summary>
        /// The Shader.PropertyToID for "_ColorCorrection".
        /// </summary>
        int shader_ColorCorrectionID;

        /// <summary>
        /// The Shader.PropertyToID for "_LUTTex".
        /// </summary>
        int shader_LUTTexID;
        
        /// <summary>
        /// The face mask texture.
        /// </summary>
        Texture2D faceMaskTexture;
        
        /// <summary>
        /// The face mask mat.
        /// </summary>
        Mat faceMaskMat;

        /// <summary>
        /// The index number of face mask data.
        /// </summary>
        int faceMaskDataIndex = 0;

        /// <summary>
        /// The detected face rect in mask mat.
        /// </summary>
        UnityEngine.Rect faceRectInMask;

        /// <summary>
        /// The detected face landmark points in mask mat.
        /// </summary>
        List<Vector2> faceLandmarkPointsInMask;

        /// <summary>
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        string haarcascade_frontalface_alt_xml_filepath;

        /// <summary>
        /// The sp_human_face_68_dat_filepath.
        /// </summary>
        string sp_human_face_68_dat_filepath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        #if UNITY_WEBGL && !UNITY_EDITOR
        IEnumerator getFilePath_Coroutine;
        #endif

        // Use this for initialization
        void Start ()
        {
            //identifikasi kotak fps monitor
            fpsMonitor = GetComponent<FpsMonitor> ();

            //untuk ngubah hasil tangkap kamera dari tekstur ke mat
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            getFilePath_Coroutine = GetFilePath ();
            StartCoroutine (getFilePath_Coroutine);
            #else
            //untuk load file xml data training deteksi wajah opencv haarcascade
            haarcascade_frontalface_alt_xml_filepath = OpenCVForUnity.UnityUtils.Utils.getFilePath ("haarcascade_frontalface_alt.xml");
            //untuk load file xml data training 68 titik wajah dlib landmark detector
            sp_human_face_68_dat_filepath = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePath ("sp_human_face_68.dat");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath ()
        {
            var getFilePathAsync_0_Coroutine = OpenCVForUnity.UnityUtils.Utils.getFilePathAsync ("haarcascade_frontalface_alt.xml", (result) => {
                haarcascade_frontalface_alt_xml_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = DlibFaceLandmarkDetector.UnityUtils.Utils.getFilePathAsync ("sp_human_face_68.dat", (result) => {
                sp_human_face_68_dat_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            getFilePath_Coroutine = null;

            Run ();
        }
        #endif

        private void Run ()
        {

            meshOverlay = this.GetComponent<TrackedMeshOverlay> ();

            // Customize face mask.
            GameObject newBaseObject = Instantiate (meshOverlay.baseObject);
            newBaseObject.name = "CustomFaceMaskTrackedMesh";
            TrackedMesh tm = newBaseObject.GetComponent<TrackedMesh> ();

            if (extendHijab) {
                ExtendHijab (tm.meshFilter.mesh);
            }

            Texture alphaMask = tm.material.GetTexture ("_MaskTex");
            Vector2[] uv = tm.meshFilter.sharedMesh.uv2;
            Texture2D newAlphaMask = CreateFaceMaskAlphaMaskTexture (alphaMask.width, alphaMask.height, uv);
            tm.material.SetTexture ("_MaskTex", newAlphaMask);

            meshOverlay.baseObject = newBaseObject;
            //end customize

            //setting shader
            shader_FadeID = Shader.PropertyToID ("_Fade");
            shader_ColorCorrectionID = Shader.PropertyToID ("_ColorCorrection");
            shader_LUTTexID = Shader.PropertyToID ("_LUTTex");

            //untuk melacak rectangle
            rectangleTracker = new RectangleTracker ();

            //mendeteksi face points berdasarkan file .dat yg memiliki 68 titik landmark
            faceLandmarkDetector = new FaceLandmarkDetector (sp_human_face_68_dat_filepath);

            //var yg dibutuhkan dalam noise filter
            lowPassFilterDict = new Dictionary<int, LowPassPointsFilter> ();
            opticalFlowFilterDict = new Dictionary<int, OFPointsFilter> ();

            //color correction pada face mask atau hijab
            faceMaskColorCorrector = new FaceMaskColorCorrector ();

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
            #endif
            

            webCamTextureToMatHelper.Initialize ();

            //pengaturan opsi toggle rect, point, dan color correctio
            displayFaceRectsToggle.isOn = displayFaceRects;
            enableColorCorrectionToggle.isOn = enableColorCorrection;
            displayDebugFacePointsToggle.isOn = displayDebugFacePoints;
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);


            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            //if (fpsMonitor != null) {
            //    fpsMonitor.Add ("width", webCamTextureMat.width ().ToString ());
            //    fpsMonitor.Add ("height", webCamTextureMat.height ().ToString ());
            //    fpsMonitor.Add ("orientation", Screen.orientation.ToString ());
            //}


            float width = gameObject.transform.localScale.x;
            float height = gameObject.transform.localScale.y;

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            grayMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            cascade = new CascadeClassifier (haarcascade_frontalface_alt_xml_filepath);

            meshOverlay.UpdateOverlayTransform (gameObject.transform);

            //OnChangeFaceMaskButtonClick ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            grayMat.Dispose ();

            if (texture != null) {
                Texture2D.Destroy (texture);
                texture = null;
            }

            rectangleTracker.Reset ();
            meshOverlay.Reset ();

            foreach (var key in lowPassFilterDict.Keys) {
                lowPassFilterDict [key].Dispose ();
            }
            lowPassFilterDict.Clear ();
            foreach (var key in opticalFlowFilterDict.Keys) {
                opticalFlowFilterDict [key].Dispose ();
            }
            opticalFlowFilterDict.Clear ();

            faceMaskColorCorrector.Reset ();

        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                // detect faces.
                List<Rect> detectResult = new List<Rect> ();
                    Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                    using (Mat equalizeHistMat = new Mat ())
                    using (MatOfRect faces = new MatOfRect ()) {
                        Imgproc.equalizeHist (grayMat, equalizeHistMat);

                        cascade.detectMultiScale (equalizeHistMat, faces, 1.2f, 5, 0 | Objdetect.CASCADE_SCALE_IMAGE, new Size (equalizeHistMat.cols () * 0.15, equalizeHistMat.cols () * 0.15), new Size ());

                        detectResult = faces.toList ();
                    }

                    // corrects the deviation of a detection result between OpenCV and Dlib.
                    foreach (Rect r in detectResult) {
                        r.y += (int)(r.height * 0.1f);
                    }              

                // face tracking.
                rectangleTracker.UpdateTrackedObjects (detectResult);
                List<TrackedRect> trackedRects = new List<TrackedRect> ();
                rectangleTracker.GetObjects (trackedRects, true);

                // create noise filter.
                foreach (var openCVRect in trackedRects) {
                    if (openCVRect.state == TrackedState.NEW) {
                        if (!lowPassFilterDict.ContainsKey (openCVRect.id))
                            lowPassFilterDict.Add (openCVRect.id, new LowPassPointsFilter ((int)faceLandmarkDetector.GetShapePredictorNumParts ()));
                        if (!opticalFlowFilterDict.ContainsKey (openCVRect.id))
                            opticalFlowFilterDict.Add (openCVRect.id, new OFPointsFilter ((int)faceLandmarkDetector.GetShapePredictorNumParts ()));
                    } else if (openCVRect.state == TrackedState.DELETED) {
                        if (lowPassFilterDict.ContainsKey (openCVRect.id)) {
                            lowPassFilterDict [openCVRect.id].Dispose ();
                            lowPassFilterDict.Remove (openCVRect.id);
                        }
                        if (opticalFlowFilterDict.ContainsKey (openCVRect.id)) {
                            opticalFlowFilterDict [openCVRect.id].Dispose ();
                            opticalFlowFilterDict.Remove (openCVRect.id);
                        }
                    }
                }

                // create LUT texture.
                foreach (var openCVRect in trackedRects) {
                    if (openCVRect.state == TrackedState.NEW) {
                        faceMaskColorCorrector.CreateLUTTex (openCVRect.id);
                    } else if (openCVRect.state == TrackedState.DELETED) {
                        faceMaskColorCorrector.DeleteLUTTex (openCVRect.id);
                    }
                }

                // detect face landmark points.
                OpenCVForUnityUtils.SetImage (faceLandmarkDetector, rgbaMat);
                List<List<Vector2>> landmarkPoints = new List<List<Vector2>> ();
                for (int i = 0; i < trackedRects.Count; i++) {
                    TrackedRect tr = trackedRects [i];
                    UnityEngine.Rect rect = new UnityEngine.Rect (tr.x, tr.y, tr.width, tr.height);

                    List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);

                    // apply noise filter.                   
                        if (tr.state > TrackedState.NEW && tr.state < TrackedState.DELETED) {
                            opticalFlowFilterDict [tr.id].Process (rgbaMat, points, points);
                            lowPassFilterDict [tr.id].Process (rgbaMat, points, points);
                        }
                    
                    if (extendHijab) {
                        AddHijabPoints (points);
                    }

                    landmarkPoints.Add (points);
                }

                // face masking.
                if (faceMaskTexture != null && landmarkPoints.Count >= 1) { // Apply face masking between detected faces and a face mask image.

                    float maskImageWidth = faceMaskTexture.width;
                    float maskImageHeight = faceMaskTexture.height;

                    TrackedRect tr;

                    for (int i = 0; i < trackedRects.Count; i++) {
                        tr = trackedRects [i];

                        //untuk rect wajah pertama
                        if (tr.state == TrackedState.NEW) {
                            meshOverlay.CreateObject (tr.id, faceMaskTexture);
                        }
                        
                        if (tr.state < TrackedState.DELETED) {
                            MaskFace (meshOverlay, tr, landmarkPoints [i], faceLandmarkPointsInMask, maskImageWidth, maskImageHeight);

                            if (enableColorCorrection) {
                                CorrectFaceMaskColor (tr.id, faceMaskMat, rgbaMat, faceLandmarkPointsInMask, landmarkPoints [i]);
                            }
                        } else if (tr.state == TrackedState.DELETED) {
                            meshOverlay.DeleteObject (tr.id);
                        }
                    }
                } else if (landmarkPoints.Count >= 1) { // Apply face masking between detected faces.

                    float maskImageWidth = texture.width;
                    float maskImageHeight = texture.height;

                    TrackedRect tr;

                    for (int i = 0; i < trackedRects.Count; i++) {
                        tr = trackedRects [i];
                        
                        if (tr.state == TrackedState.NEW) {
                            meshOverlay.CreateObject (tr.id, texture);
                        }
                        if (tr.state < TrackedState.DELETED) {
                            MaskFace (meshOverlay, tr, landmarkPoints [i], landmarkPoints [0], maskImageWidth, maskImageHeight);

                            if (enableColorCorrection) {
                                CorrectFaceMaskColor (tr.id, rgbaMat, rgbaMat, landmarkPoints [0], landmarkPoints [i]);
                            }
                        } else if (tr.state == TrackedState.DELETED) {
                            meshOverlay.DeleteObject (tr.id);
                        }
                    }
                }

                // draw face rects.
                if (displayFaceRects) {
                    for (int i = 0; i < detectResult.Count; i++) {
                        UnityEngine.Rect rect = new UnityEngine.Rect (detectResult [i].x, detectResult [i].y, detectResult [i].width, detectResult [i].height);
                        OpenCVForUnityUtils.DrawFaceRect (rgbaMat, rect, new Scalar (255, 0, 0, 255), 2);
                    }
                    //lebih smooth, jadi detectResult setelah diperhalus jadi trackedRect
                    for (int i = 0; i < trackedRects.Count; i++) {
                        UnityEngine.Rect rect = new UnityEngine.Rect (trackedRects [i].x, trackedRects [i].y, trackedRects [i].width, trackedRects [i].height);
                        OpenCVForUnityUtils.DrawFaceRect (rgbaMat, rect, new Scalar (255, 255, 0, 255), 2);
                    }
                }

                // draw face points.
                if (displayDebugFacePoints) {
                    for (int i = 0; i < landmarkPoints.Count; i++) {
                        DrawFaceLandmark (rgbaMat, landmarkPoints [i], new Scalar (0, 0, 255, 255), 2);
                    }
                }

                OpenCVForUnity.UnityUtils.Utils.fastMatToTexture2D (rgbaMat, texture);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Quit the application
                Application.Quit();
            }
        }

        private void MaskFace (TrackedMeshOverlay meshOverlay, TrackedRect tr, List<Vector2> landmarkPoints, List<Vector2> landmarkPointsInMaskImage, float maskImageWidth = 0, float maskImageHeight = 0)
        {
            float imageWidth = meshOverlay.width;
            float imageHeight = meshOverlay.height; 

            if (maskImageWidth == 0)
                maskImageWidth = imageWidth;

            if (maskImageHeight == 0)
                maskImageHeight = imageHeight;

            TrackedMesh tm = meshOverlay.GetObjectById (tr.id);

            Vector3[] vertices = tm.meshFilter.mesh.vertices;
            if (vertices.Length == landmarkPoints.Count) {
                for (int j = 0; j < vertices.Length; j++) {
                    vertices [j].x = landmarkPoints [j].x / imageWidth - 0.5f;
                    vertices [j].y = 0.5f - landmarkPoints [j].y / imageHeight;
                }
            }
            Vector2[] uv = tm.meshFilter.mesh.uv;
            if (uv.Length == landmarkPointsInMaskImage.Count) {
                for (int jj = 0; jj < uv.Length; jj++) {
                    uv [jj].x = landmarkPointsInMaskImage [jj].x / maskImageWidth;
                    uv [jj].y = (maskImageHeight - landmarkPointsInMaskImage [jj].y) / maskImageHeight;
                }
            }
            meshOverlay.UpdateObject (tr.id, vertices, null, uv);

            if (tr.numFramesNotDetected > 3) {
                tm.sharedMaterial.SetFloat (shader_FadeID, 1f);
            } else if (tr.numFramesNotDetected > 0 && tr.numFramesNotDetected <= 3) {
                tm.sharedMaterial.SetFloat (shader_FadeID, 0.3f + (0.7f / 4f) * tr.numFramesNotDetected);
            } else {
                tm.sharedMaterial.SetFloat (shader_FadeID, 0.3f);
            }

            if (enableColorCorrection) {
                tm.sharedMaterial.SetFloat (shader_ColorCorrectionID, 1f);
            } else {
                tm.sharedMaterial.SetFloat (shader_ColorCorrectionID, 0f);
            }
                          
        }

        private void CorrectFaceMaskColor (int id, Mat src, Mat dst, List<Vector2> src_landmarkPoints, List<Vector2> dst_landmarkPoints)
        {
            Texture2D LUTTex = faceMaskColorCorrector.UpdateLUTTex (id, src, dst, src_landmarkPoints, dst_landmarkPoints);
            TrackedMesh tm = meshOverlay.GetObjectById (id);
            tm.sharedMaterial.SetTexture (shader_LUTTexID, LUTTex);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            webCamTextureToMatHelper.Dispose ();

            if (cascade != null)
                cascade.Dispose ();

            if (rectangleTracker != null)
                rectangleTracker.Dispose ();

            if (faceLandmarkDetector != null)
                faceLandmarkDetector.Dispose ();

            foreach (var key in lowPassFilterDict.Keys) {
                lowPassFilterDict [key].Dispose ();
            }
            lowPassFilterDict.Clear ();
            foreach (var key in opticalFlowFilterDict.Keys) {
                opticalFlowFilterDict [key].Dispose ();
            }
            opticalFlowFilterDict.Clear ();

            if (faceMaskColorCorrector != null)
                faceMaskColorCorrector.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            if (getFilePath_Coroutine != null) {
                StopCoroutine (getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose ();
            }
            #endif
        }
        
        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing ();
        }
       
        /// <summary>
        /// Raises the enable color correction toggle value changed event.
        /// </summary>
        public void OnEnableColorCorrectionToggleValueChanged ()
        {
            if (enableColorCorrectionToggle.isOn) {
                enableColorCorrection = true;
            } else {
                enableColorCorrection = false;
            }
        }

        /// <summary>
        /// Raises the display face rects toggle value changed event.
        /// </summary>
        public void OnDisplayFaceRectsToggleValueChanged ()
        {
            if (displayFaceRectsToggle.isOn) {
                displayFaceRects = true;
            } else {
                displayFaceRects = false;
            }
        }

        /// <summary>
        /// Raises the display debug face points toggle value changed event.
        /// </summary>
        public void OnDisplayDebugFacePointsToggleValueChanged ()
        {
            if (displayDebugFacePointsToggle.isOn) {
                displayDebugFacePoints = true;
            } else {
                displayDebugFacePoints = false;
            }
        }

        /// <summary>
        /// Raises the change face mask button click event.
        /// </summary>
        public void OnChangeFaceMaskButtonClick ()
        {
            RemoveHijab();
            if (faceMaskDatas.Count == 0)
                return;

            FaceMaskData maskData = faceMaskDatas [faceMaskDataIndex];
            faceMaskDataIndex = (faceMaskDataIndex < faceMaskDatas.Count - 1) ? faceMaskDataIndex + 1 : 0;

            if (maskData == null) {
                Debug.LogError ("maskData == null");
                return;
            }

            if (maskData.image == null) {
                Debug.LogError ("image == null");
                return;
            }

            if (maskData.landmarkPoints.Count != 68) {
                Debug.LogError ("landmarkPoints.Count != 68");
                return;
            }

            faceMaskTexture = maskData.image;
            faceMaskMat = new Mat (faceMaskTexture.height, faceMaskTexture.width, CvType.CV_8UC4);
            OpenCVForUnity.UnityUtils.Utils.texture2DToMat (faceMaskTexture, faceMaskMat);

            if (!maskData.isDynamicMode) {
                /*faceRectInMask = DetectFace (faceMaskMat);
                faceLandmarkPointsInMask = DetectFaceLandmarkPoints (faceMaskMat, faceRectInMask);

                maskData.faceRect = faceRectInMask;
                maskData.landmarkPoints = faceLandmarkPointsInMask;*/
                faceRectInMask = maskData.faceRect;
                faceLandmarkPointsInMask = maskData.landmarkPoints;
            } 

            if (extendHijab) {
                List<Vector2> newLandmarkPointsInMask = new List<Vector2> (faceLandmarkPointsInMask);
                AddHijabPoints (newLandmarkPointsInMask);
                faceLandmarkPointsInMask = newLandmarkPointsInMask;
            }

            if (faceRectInMask.width == 0 && faceRectInMask.height == 0) {
                RemoveHijab ();
                Debug.LogError ("A face could not be detected from the input image.");
            }

            enableColorCorrectionToggle.isOn = maskData.enableColorCorrection;
        }

        /// <summary>
        /// Raises the remove face mask button click event.
        /// </summary>
        public void OnRemoveFaceMaskButtonClick ()
        {
            RemoveHijab ();
        }

        private void RemoveHijab ()
        {
            faceMaskTexture = null;
            if (faceMaskMat != null)
            {
                faceMaskMat.Dispose();
                faceMaskMat = null;
            }

            rectangleTracker.Reset();
            meshOverlay.Reset();
        }

        /*private UnityEngine.Rect DetectFace (Mat mat)
        {
                
                using (Mat grayMat = new Mat ())
                using (Mat equalizeHistMat = new Mat ())
                using (MatOfRect faces = new MatOfRect ()) {
                    // convert image to greyscale.
                    Imgproc.cvtColor (mat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                    Imgproc.equalizeHist (grayMat, equalizeHistMat);
                    
                    cascade.detectMultiScale (equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new Size (equalizeHistMat.cols () * 0.15, equalizeHistMat.cols () * 0.15), new Size ());

                    List<Rect> faceList = faces.toList ();
                    if (faceList.Count >= 1) {
                        UnityEngine.Rect r = new UnityEngine.Rect (faceList [0].x, faceList [0].y, faceList [0].width, faceList [0].height);
                        // corrects the deviation of a detection result between OpenCV and Dlib.
                        r.y += (int)(r.height * 0.1f);
                        return r;
                    }
                }
            return new UnityEngine.Rect ();
        }

        private List<Vector2> DetectFaceLandmarkPoints (Mat mat, UnityEngine.Rect rect)
        {
            OpenCVForUnityUtils.SetImage (faceLandmarkDetector, mat);
            List<Vector2> points = faceLandmarkDetector.DetectLandmark (rect);

            return points;
        }
*/
        private void ExtendHijab(Mesh mesh)
        {
            if (mesh.vertices.Length != 68)
                throw new ArgumentException("Invalid face mask mesh", "mesh");

            List<Vector2> verticesList = new List<Vector2>(mesh.vertices.Length);
            foreach (Vector3 v in mesh.vertices)
            {
                verticesList.Add(new Vector2(v.x, v.y));
            }

            AddHijabPoints(verticesList);

            Vector3[] vertices = new Vector3[verticesList.Count];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(verticesList[i].x, verticesList[i].y);
            }
            mesh.vertices = vertices;

            int[] HijabTriangles = new int[39 * 3] {
                
                //6
                68, 16, 26,
                68, 26, 25,
                69, 68, 25,
                69, 25, 24,
                69, 24, 23,
                70, 69, 23,

                //7
                70, 23, 20,

                //13
                71, 70, 20,
                71, 20, 19,
                71, 19, 18,
                72, 71, 18,
                72, 18, 17,
                72, 17, 0,

                //15
                73, 72, 0,
                73, 0, 1,

                //23
                74, 73, 1,
                74, 1, 2,
                74, 2, 3,
                74, 3, 4,
                74, 4, 5,
                74, 5, 6,
                74, 6, 7,
                74, 7, 8,

                //26
                75, 74, 8,
                76, 75, 8,
                76, 8, 77,
                                
                //29
                77, 8, 78,
                78, 8, 79,
                79, 8, 80,
                                              
                //37
                8, 9, 80,
                9, 10, 80,
                10, 11, 80,
                11, 12, 80,
                12, 13, 80,
                13, 14, 80,
                14, 15, 80,
                15, 16, 80,
                
                //39
                16, 68, 81,
                16, 81, 80
            };
            
            mesh.triangles = HijabTriangles;

            Vector2[] uv = new Vector2[mesh.uv.Length];
            for (int j = 0; j < uv.Length; j++)
            {
                uv[j].x = vertices[j].x + 0.5f;
                uv[j].y = vertices[j].y + 0.5f;
            }
            mesh.uv = uv;
            mesh.uv2 = (Vector2[])uv.Clone();

            mesh.RecalculateNormals();
        }

        //add point for hijab        
        private void AddHijabPoints(List<Vector2> points)
        {
            if (points.Count != 68)
                throw new ArgumentException("Invalid face landmark points", "points");

            Vector2 noseLength = new Vector2(points[27].x - points[30].x, points[27].y - points[30].y);
            Vector2 glabellaPoint = new Vector2((points[19].x + points[24].x) / 2f, (points[19].y + points[24].y) / 2f);
            Vector2 glabellaLength = new Vector2(points[20].x - points[23].x, points[20].y - points[23].y);


            points.Add(new Vector2(points[26].x - noseLength.x * 2f - glabellaLength.x * 0.5f, points[24].y + noseLength.y * 1.5f));//68
            points.Add(new Vector2(points[24].x, points[24].y + noseLength.y * 2f));//69
            points.Add(new Vector2(glabellaPoint.x + noseLength.x * 0.8f, glabellaPoint.y + noseLength.y * 2.3f)); //70
            points.Add(new Vector2(points[19].x, points[19].y + noseLength.y * 2f));//71
            points.Add(new Vector2(points[17].x + noseLength.x * 2f + glabellaLength.x * 0.5f, points[17].y + noseLength.y * 1.8f));//72
            //5

            //point hijab
            points.Add(new Vector2(points[0].x + noseLength.x * 2f + glabellaLength.x * 0.7f, points[0].y));//73
            points.Add(new Vector2(points[0].x + noseLength.x * 2f + glabellaLength.x * 1.5f, points[8].y));//74
            //7

            points.Add(new Vector2(points[0].x + noseLength.x * 5f + glabellaLength.x * 2f, points[8].y - noseLength.y * 0.8f));//75
            points.Add(new Vector2(points[0].x + noseLength.x * 5f + glabellaLength.x * 3f, points[8].y - noseLength.y * 2f));//76
            //9

            points.Add(new Vector2(points[8].x, points[8].y - noseLength.y * 2.5f));//77
            //10

            points.Add(new Vector2(points[16].x - noseLength.x * 5f - glabellaLength.x * 3f, points[8].y - noseLength.y * 2f));//78
            points.Add(new Vector2(points[16].x - noseLength.x * 5f - glabellaLength.x * 2f, points[8].y - noseLength.y * 0.8f));//79

            points.Add(new Vector2(points[16].x - noseLength.x * 2f - glabellaLength.x * 1.5f, points[8].y));//80
            //13

            points.Add(new Vector2(points[16].x - noseLength.x * 2f - glabellaLength.x * 0.7f, points[16].y));//81
            //14

            points.Add(new Vector2(points[26].x - noseLength.x * 2f - glabellaLength.x * 0.5f, points[24].y + noseLength.y * 1.5f));//82 = point 68
            //15
        }

        private static void DrawFaceLandmark (Mat imgMat, List<Vector2> points, Scalar color, int thickness)
        {
            if (points.Count == 83) { 
                //draw face line
                for (int i = 1; i <= 16; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), color, thickness);
                
                //nose line
                for (int i = 28; i <= 30; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), color, thickness);
                
                //draw eyebrows lines
                for (int i = 18; i <= 21; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), color, thickness);
                for (int i = 23; i <= 26; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), color, thickness);
                
                //nose line
                for (int i = 31; i <= 35; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), color, thickness);               
                Imgproc.line (imgMat, new Point (points [30].x, points [30].y), new Point (points [35].x, points [35].y), color, thickness);
                
                //right eye line
                for (int i = 37; i <= 41; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), color, thickness);
                Imgproc.line (imgMat, new Point (points [36].x, points [36].y), new Point (points [41].x, points [41].y), color, thickness);
                
                //left eye line
                for (int i = 43; i <= 47; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), color, thickness);
                Imgproc.line (imgMat, new Point (points [42].x, points [42].y), new Point (points [47].x, points [47].y), color, thickness);
                
                //mouth line
                for (int i = 49; i <= 59; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), color, thickness);               
                Imgproc.line (imgMat, new Point (points [48].x, points [48].y), new Point (points [59].x, points [59].y), color, thickness);
                
                for (int i = 61; i <= 67; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), color, thickness);
                Imgproc.line (imgMat, new Point (points [60].x, points [60].y), new Point (points [67].x, points [67].y), color, thickness);
                
                //draw hijab line
                for (int i = 69; i <= 82; ++i)
                    Imgproc.line (imgMat, new Point (points [i].x, points [i].y), new Point (points [i - 1].x, points [i - 1].y), new Scalar (0, 255, 0, 255), thickness);

            }
            
        }

        private Texture2D CreateFaceMaskAlphaMaskTexture (float width, float height, Vector2[] uv)
        {
            if (uv.Length != 83)
                throw new ArgumentException ("Invalid landmark points", "uv");

            Vector2[] hijabContourUVPoints = new Vector2[] {
                    uv [73],
                    uv [74],
                    uv [75],
                    uv [76],

                    uv [77],

                    uv [78],
                    uv [79],
                    uv [80],
                    uv [81],

                    uv [68],                    
                    uv [69],
                    uv [70],
                    uv [71],
                    uv [72]
                };          

            Vector2[][] exclusionAreas = new Vector2[][]{ };

            return AlphaMaskTextureCreater.CreateAlphaMaskTexture (width * 2f, height, hijabContourUVPoints, exclusionAreas);
        }
    }
}