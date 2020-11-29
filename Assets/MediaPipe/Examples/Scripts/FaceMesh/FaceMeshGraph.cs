using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FaceMeshGraph : DemoGraph {
  [SerializeField] int numFaces = 1;

  private const string multiFaceLandmarksStream = "multi_face_landmarks";
  private const string faceRectsFromLandmarksStream = "face_rects_from_landmarks";
  private const string faceDetectionsStream = "face_detections";

  private SidePacket sidePacket;
  private Stack<List<NormalizedLandmarkList>> multiFaceLandmarkLists;
  private Stack<List<NormalizedRect>> faceRectLists;
  private Stack<List<Detection>> faceDetectionLists;
  private GCHandle multiFaceLandmarksCallbackHandle;
  private GCHandle faceRectsFromLandmarksCallbackHandle;
  private GCHandle faceDetectionsCallbackHandle;

  public override Status StartRun() {
    multiFaceLandmarkLists = new Stack<List<NormalizedLandmarkList>>();
    graph.ObserveOutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>>(
        multiFaceLandmarksStream, MultiFaceLandmarksCallback, out multiFaceLandmarksCallbackHandle).AssertOk();

    faceRectLists = new Stack<List<NormalizedRect>>();
    graph.ObserveOutputStream<NormalizedRectVectorPacket, List<NormalizedRect>>(
        faceRectsFromLandmarksStream, FaceRectsFromLandmarksCallback, out faceRectsFromLandmarksCallbackHandle).AssertOk();

    faceDetectionLists = new Stack<List<Detection>>();
    graph.ObserveOutputStream<DetectionVectorPacket, List<Detection>>(
        faceDetectionsStream, FaceDetectionsCallback, out faceDetectionsCallbackHandle).AssertOk();

    sidePacket = new SidePacket();
    sidePacket.Emplace("num_faces", new IntPacket(numFaces));

    return graph.StartRun(sidePacket);
  }

  protected override void OnDestroy() {
    base.OnDestroy();

    if (multiFaceLandmarksCallbackHandle.IsAllocated) {
      multiFaceLandmarksCallbackHandle.Free();
    }

    if (faceRectsFromLandmarksCallbackHandle.IsAllocated) {
      faceRectsFromLandmarksCallbackHandle.Free();
    }

    if (faceDetectionsCallbackHandle.IsAllocated) {
      faceDetectionsCallbackHandle.Free();
    }
  }

  public override void RenderOutput(WebCamScreenController screenController, TextureFrame textureFrame) {
    var faceMeshValue = FetchNextFaceMeshValue();
    RenderAnnotation(screenController, faceMeshValue);

    screenController.DrawScreen(textureFrame);
  }

  private FaceMeshValue FetchNextFaceMeshValue() {
    List<NormalizedLandmarkList> multiFaceLandmarks = null;

    lock (((ICollection)multiFaceLandmarkLists).SyncRoot) {
      if (multiFaceLandmarkLists.Count > 0) {
        multiFaceLandmarks = multiFaceLandmarkLists.Peek();
        multiFaceLandmarkLists.Clear();
      }
    }

    if (multiFaceLandmarks == null) {
      // face not found
      return new FaceMeshValue();
    }

    List<NormalizedRect> faceRects = null;

    lock (((ICollection)faceRectLists).SyncRoot) {
      if (faceRectLists.Count > 0) {
        faceRects = faceRectLists.Peek();
        faceRectLists.Clear();
      }
    }

    if (faceRects == null) {
      return new FaceMeshValue(multiFaceLandmarks);
    }

    List<Detection> faceDetections = null;

    lock (((ICollection)faceDetectionLists).SyncRoot) {
      if (faceDetectionLists.Count > 0) {
        faceDetections = faceDetectionLists.Peek();
        faceDetectionLists.Clear();
      }
    }

    if (faceDetections == null) {
      return new FaceMeshValue(multiFaceLandmarks, faceRects);
    }

    return new FaceMeshValue(multiFaceLandmarks, faceRects, faceDetections);
  }

  private void RenderAnnotation(WebCamScreenController screenController, FaceMeshValue value) {
    // NOTE: input image is flipped
    GetComponent<FaceMeshAnnotationController>().Draw(
        screenController.transform, value.MultiFaceLandmarks, value.FaceRectsFromLandmarks, value.FaceDetections, true);
  }

  private Status MultiFaceLandmarksCallback(NormalizedLandmarkListVectorPacket packet) {
    var value = packet.Get();

    lock (((ICollection)multiFaceLandmarkLists).SyncRoot) {
      multiFaceLandmarkLists.Push(value);
    }

    return Status.Ok();
  }

  private Status FaceRectsFromLandmarksCallback(NormalizedRectVectorPacket packet) {
    var value = packet.Get();

    lock (((ICollection)faceRectLists).SyncRoot) {
      faceRectLists.Push(value);
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
