using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class FaceDetectionGraph : DemoGraph {
  private const string faceDetectionsStream = "face_detections";

  private Stack<List<Detection>> detectionLists;
  private GCHandle faceDetectionsCallbackHandle;

  public override Status StartRun() {
    detectionLists = new Stack<List<Detection>>();
    graph.ObserveOutputStream<DetectionVectorPacket, List<Detection>>(
        faceDetectionsStream, FaceDetectionsCallback, out faceDetectionsCallbackHandle).AssertOk();

    return graph.StartRun();
  }

  protected override void OnDestroy() {
    base.OnDestroy();

    if (faceDetectionsCallbackHandle.IsAllocated) {
      faceDetectionsCallbackHandle.Free();
    }
  }

  public override void RenderOutput(WebCamScreenController screenController, TextureFrame textureFrame) {
    List<Detection> detections = null;

    lock (((ICollection)detectionLists).SyncRoot) {
      if (detectionLists.Count > 0) {
        detections = detectionLists.Peek();
        detectionLists.Clear();
      }
    }

    RenderAnnotation(screenController, detections == null ? new List<Detection>() : detections);

    screenController.DrawScreen(textureFrame);
  }

  private void RenderAnnotation(WebCamScreenController screenController, List<Detection> detections) {
    // NOTE: input image is flipped
    GetComponent<DetectionListAnnotationController>().Draw(screenController.transform, detections, true);
  }

  private Status FaceDetectionsCallback(DetectionVectorPacket packet) {
    var detections = packet.Get();

    lock (((ICollection)detectionLists).SyncRoot) {
      detectionLists.Push(detections);
    }

    return Status.Ok();
  }
}
