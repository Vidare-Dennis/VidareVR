using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class IrisTrackingGraph : DemoGraph {
  private const string faceLandmarksWithIrisStream = "face_landmarks_with_iris";
  private const string faceRectStream = "face_rect";
  private const string faceDetectionsStream = "face_detections";

  private Stack<NormalizedLandmarkList> faceLandmarksWithIrises;
  private Stack<NormalizedRect> faceRects;
  private Stack<List<Detection>> faceDetectionLists;
  private GCHandle faceLandmarksWithIrisCallbackHandle;
  private GCHandle faceRectCallbackHandle;
  private GCHandle faceDetectionsCallbackHandle;


  public override Status StartRun() {
    faceLandmarksWithIrises = new Stack<NormalizedLandmarkList>();
    graph.ObserveOutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(
        faceLandmarksWithIrisStream, FaceLandmarksWithIrisCallback, out faceLandmarksWithIrisCallbackHandle).AssertOk();

    faceRects = new Stack<NormalizedRect>();
    graph.ObserveOutputStream<NormalizedRectPacket, NormalizedRect>(faceRectStream, FaceRectCallback, out faceRectCallbackHandle).AssertOk();

    faceDetectionLists = new Stack<List<Detection>>();
    graph.ObserveOutputStream<DetectionVectorPacket, List<Detection>>(
        faceDetectionsStream, FaceDetectionsCallback, out faceDetectionsCallbackHandle).AssertOk();

    return graph.StartRun();
  }

  protected override void OnDestroy() {
    base.OnDestroy();

    if (faceLandmarksWithIrisCallbackHandle.IsAllocated) {
      faceLandmarksWithIrisCallbackHandle.Free();
    }

    if (faceRectCallbackHandle.IsAllocated) {
      faceRectCallbackHandle.Free();
    }

    if (faceDetectionsCallbackHandle.IsAllocated) {
      faceDetectionsCallbackHandle.Free();
    }
  }

  public override void RenderOutput(WebCamScreenController screenController, TextureFrame textureFrame) {
    var faceMeshValue = FetchNextIrisTrackingValue();
    RenderAnnotation(screenController, faceMeshValue);

    screenController.DrawScreen(textureFrame);
  }

  private IrisTrackingValue FetchNextIrisTrackingValue() {
    NormalizedLandmarkList faceLandmarksWithIris = null;

    lock (((ICollection)faceLandmarksWithIrises).SyncRoot) {
      if (faceLandmarksWithIrises.Count > 0) {
        faceLandmarksWithIris = faceLandmarksWithIrises.Peek();
        faceLandmarksWithIrises.Clear();
      }
    }

    NormalizedRect faceRect = null;

    lock (((ICollection)faceRects).SyncRoot) {
      if (faceRects.Count > 0) {
        faceRect = faceRects.Peek();
        faceRects.Clear();
      }
    }

    List<Detection> faceDetections = null;

    lock (((ICollection)faceDetectionLists).SyncRoot) {
      if (faceDetectionLists.Count > 0) {
        faceDetections = faceDetectionLists.Peek();
        faceDetectionLists.Clear();
      }
    }

    if (faceDetections == null) {
      return new IrisTrackingValue(faceLandmarksWithIris, faceRect);
    }

    return new IrisTrackingValue(faceLandmarksWithIris, faceRect, faceDetections);
  }

  private void RenderAnnotation(WebCamScreenController screenController, IrisTrackingValue value) {
    // NOTE: input image is flipped
    GetComponent<IrisTrackingAnnotationController>().Draw(
        screenController.transform, value.FaceLandmarksWithIris, value.FaceRect, value.FaceDetections, true);
  }

  private Status FaceLandmarksWithIrisCallback(NormalizedLandmarkListPacket packet) {
    var value = packet.Get();

    lock (((ICollection)faceLandmarksWithIrises).SyncRoot) {
      faceLandmarksWithIrises.Push(value);
    }

    return Status.Ok();
  }

  private Status FaceRectCallback(NormalizedRectPacket packet) {
    var value = packet.Get();

    lock (((ICollection)faceRects).SyncRoot) {
      faceRects.Push(value);
    }

    return Status.Ok();
  }

  private Status FaceDetectionsCallback(DetectionVectorPacket packet) {
    var value = packet.Get();

    lock (((ICollection)faceDetectionLists).SyncRoot) {
      faceDetectionLists.Push(value);
    }

    return Status.Ok();
  }
}
