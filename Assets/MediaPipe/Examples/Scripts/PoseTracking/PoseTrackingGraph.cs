using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class PoseTrackingGraph : DemoGraph {
  private const string poseLandmarksStream = "pose_landmarks_smoothed";
  private const string poseDetectionStream = "pose_detection";

  private Stack<NormalizedLandmarkList> poseLandmarkLists;
  private Stack<Detection> poseDetections;
  private GCHandle poseLandmarksCallbackHandle;
  private GCHandle poseDetectionCallbackHandle;

  public override Status StartRun() {
    poseLandmarkLists = new Stack<NormalizedLandmarkList>();
    graph.ObserveOutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(
        poseLandmarksStream, PoseLandmarksCallback, out poseLandmarksCallbackHandle).AssertOk();

    poseDetections = new Stack<Detection>();
    graph.ObserveOutputStream<DetectionPacket, Detection>(
        poseDetectionStream, PoseDetectionCallback, out poseDetectionCallbackHandle).AssertOk();

    return graph.StartRun();
  }

  protected override void OnDestroy() {
    base.OnDestroy();

    if (poseLandmarksCallbackHandle.IsAllocated) {
      poseLandmarksCallbackHandle.Free();
    }

    if (poseDetectionCallbackHandle.IsAllocated) {
      poseDetectionCallbackHandle.Free();
    }
  }

  public override void RenderOutput(WebCamScreenController screenController, TextureFrame textureFrame) {
    var poseTrackingValue = FetchNextPoseTrackingValue();
    RenderAnnotation(screenController, poseTrackingValue);

    screenController.DrawScreen(textureFrame);
  }

  private PoseTrackingValue FetchNextPoseTrackingValue() {
    NormalizedLandmarkList poseLandmarks = null;

    lock (((ICollection)poseLandmarkLists).SyncRoot) {
      if (poseLandmarkLists.Count > 0) {
        poseLandmarks = poseLandmarkLists.Peek();
        poseLandmarkLists.Clear();
      }
    }

    if (poseLandmarks == null) {
      return new PoseTrackingValue();
    }

    Detection poseDetection = null;

    lock (((ICollection)poseDetections).SyncRoot) {
      if (poseDetections.Count > 0) {
        poseDetection = poseDetections.Peek();
        poseDetections.Clear();
      }
    }

    if (poseDetection == null) {
      return new PoseTrackingValue(poseLandmarks);
    }

    return new PoseTrackingValue(poseLandmarks, poseDetection);
  }

  private void RenderAnnotation(WebCamScreenController screenController, PoseTrackingValue value) {
    // NOTE: input image is flipped
    GetComponent<PoseTrackingAnnotationController>().Draw(screenController.transform, value.PoseLandmarkList, value.PoseDetection, true);
  }

  private Status PoseLandmarksCallback(NormalizedLandmarkListPacket packet) {
    var value = packet.Get();

    lock (((ICollection)poseLandmarkLists).SyncRoot) {
      poseLandmarkLists.Push(value);
    }

    return Status.Ok();
  }

  private Status PoseDetectionCallback(DetectionPacket packet) {
    var value = packet.Get();

    lock (((ICollection)poseDetections).SyncRoot) {
      poseDetections.Push(value);
    }

    return Status.Ok();
  }
}
