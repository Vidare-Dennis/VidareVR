using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class HandTrackingGraph : DemoGraph {
  private const string handLandmarksStream = "hand_landmarks";
  private const string handednessStream = "handedness";
  private const string palmDetectionsStream = "palm_detections";
  private const string palmRectsStream = "hand_rects_from_palm_detections";

  private SidePacket sidePacket;
  private Stack<List<NormalizedLandmarkList>> handLandmarkLists;
  private Stack<List<ClassificationList>> handednessLists;
  private Stack<List<Detection>> palmDetectionLists;
  private Stack<List<NormalizedRect>> palmRectLists;
  private GCHandle handLandmarksCallbackHandle;
  private GCHandle handednessCallbackHandle;
  private GCHandle palmDetectionsCallbackHandle;
  private GCHandle palmRectsCallbackHandle;

  public override Status StartRun() {
    handLandmarkLists = new Stack<List<NormalizedLandmarkList>>();
    graph.ObserveOutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>>(
        handLandmarksStream, HandLandmarksCallback, out handLandmarksCallbackHandle).AssertOk();

    handednessLists = new Stack<List<ClassificationList>>();
    graph.ObserveOutputStream<ClassificationListVectorPacket, List<ClassificationList>>(
        handednessStream, HandednessCallback, out handednessCallbackHandle).AssertOk();

    palmDetectionLists = new Stack<List<Detection>>();
    graph.ObserveOutputStream<DetectionVectorPacket, List<Detection>>(
        palmDetectionsStream, PalmDetectionsCallback, out palmDetectionsCallbackHandle).AssertOk();

    palmRectLists = new Stack<List<NormalizedRect>>();
    graph.ObserveOutputStream<NormalizedRectVectorPacket, List<NormalizedRect>>(
        palmRectsStream, PalmRectsCallback, out palmRectsCallbackHandle).AssertOk();

    sidePacket = new SidePacket();
    sidePacket.Emplace("num_hands", new IntPacket(2));

    return graph.StartRun(sidePacket);
  }

  protected override void OnDestroy() {
    base.OnDestroy();

    if (handLandmarksCallbackHandle.IsAllocated) {
      handLandmarksCallbackHandle.Free();
    }

    if (handednessCallbackHandle.IsAllocated) {
      handednessCallbackHandle.Free();
    }

    if (palmDetectionsCallbackHandle.IsAllocated) {
      palmDetectionsCallbackHandle.Free();
    }

    if (palmRectsCallbackHandle.IsAllocated) {
      palmRectsCallbackHandle.Free();
    }
  }

  public override void RenderOutput(WebCamScreenController screenController, TextureFrame textureFrame) {
    var handTrackingValue = FetchNextHandTrackingValue();
    RenderAnnotation(screenController, handTrackingValue);

    screenController.DrawScreen(textureFrame);
  }

  private HandTrackingValue FetchNextHandTrackingValue() {
    var handLandmarks = FetchNextHandLandmarks();
    var handednesses = FetchNextHandednesses();
    var palmDetections = FetchNextPalmDetections();
    var palmRects = FetchNextPalmRects();

    return new HandTrackingValue(handLandmarks, handednesses, palmDetections, palmRects);
  }

  private List<NormalizedLandmarkList> FetchNextHandLandmarks() {
    List<NormalizedLandmarkList> handLandmarks = null;

    lock (((ICollection)handLandmarkLists).SyncRoot) {
      if (handLandmarkLists.Count > 0) {
        handLandmarks = handLandmarkLists.Peek();
        handLandmarkLists.Clear();
      }
    }

    return handLandmarks == null ? new List<NormalizedLandmarkList>() : handLandmarks;
  }

  private List<ClassificationList> FetchNextHandednesses() {
    List<ClassificationList> handednessList = null;

    lock (((ICollection)handednessLists).SyncRoot) {
      if (handednessLists.Count > 0) {
        handednessList = handednessLists.Peek();
        handednessLists.Clear();
      }
    }

    return handednessList == null ? new List<ClassificationList>() : handednessList;
  }

  private List<Detection> FetchNextPalmDetections() {
    List<Detection> palmDetections = null;

    lock (((ICollection)palmDetectionLists).SyncRoot) {
      if (palmDetectionLists.Count > 0) {
        palmDetections = palmDetectionLists.Peek();
        palmDetectionLists.Clear();
      }
    }

    return palmDetections == null ? new List<Detection>() : palmDetections;
  }

  private List<NormalizedRect> FetchNextPalmRects() {
    List<NormalizedRect> palmRects = null;

    lock (((ICollection)palmRectLists).SyncRoot) {
      if (palmRectLists.Count > 0) {
        palmRects = palmRectLists.Peek();
        palmRectLists.Clear();
      }
    }

    return palmRects == null ? new List<NormalizedRect>() : palmRects;
  }

  private void RenderAnnotation(WebCamScreenController screenController, HandTrackingValue value) {
    // NOTE: input image is flipped
    GetComponent<HandTrackingAnnotationController>().Draw(
      screenController.transform, value.HandLandmarkLists, value.Handednesses, value.PalmDetections, value.PalmRects, true);
  }

  private Status HandLandmarksCallback(NormalizedLandmarkListVectorPacket packet) {
    var value = packet.Get();

    lock (((ICollection)handLandmarkLists).SyncRoot) {
      handLandmarkLists.Push(value);
    }

    return Status.Ok();
  }

  private Status HandednessCallback(ClassificationListVectorPacket packet) {
    var value = packet.Get();

    lock (((ICollection)handednessLists).SyncRoot) {
      handednessLists.Push(value);
    }

    return Status.Ok();
  }

  private Status PalmDetectionsCallback(DetectionVectorPacket packet) {
    var value = packet.Get();

    lock (((ICollection)palmDetectionLists).SyncRoot) {
      palmDetectionLists.Push(value);
    }

    return Status.Ok();
  }

  private Status PalmRectsCallback(NormalizedRectVectorPacket packet) {
    var value = packet.Get();

    lock (((ICollection)palmRectLists).SyncRoot) {
      palmRectLists.Push(value);
    }

    return Status.Ok();
  }
}
