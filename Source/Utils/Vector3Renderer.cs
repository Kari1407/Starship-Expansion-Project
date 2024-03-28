using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace TundraExploration.Utils
{
    public class Vector3Renderer
    {
        public GameObject gameObject;

        private LineRenderer lineRenderer;
        private RectTransform labelTransform;
        private GameObject labelCanvas;
        private Text label;

        public Vector3Renderer(Part part, string name, string displayName, Color color)
        {
            gameObject = new GameObject(name);
            gameObject.transform.SetParent(part.transform);

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = lineRenderer.endColor = Color.white;
            lineRenderer.positionCount = 4;
            lineRenderer.widthMultiplier = 0.12f;
            lineRenderer.startColor = lineRenderer.endColor = color;

            labelCanvas = new GameObject("labelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            labelCanvas.AddComponent<CanvasRenderer>();

            label = labelCanvas.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            label.fontSize = 60;
            label.alignment = TextAnchor.UpperCenter;
            label.fontStyle = FontStyle.Bold;
            label.text = displayName;

            labelTransform = (RectTransform)labelCanvas.transform;
            labelTransform.SetParent(gameObject.transform, false);
            labelTransform.sizeDelta = new Vector2(1000, 200);
            labelTransform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            gameObject.SetActive(false);
        }

        public void DrawLine(Transform transform, Vector3 direction, float length, bool enabled)
        {
            gameObject.SetActive(enabled);

            if (!enabled)
                return;

            Camera camera = FlightCamera.fetch.mainCamera;

            labelTransform.position = transform.position + direction * (length / 2);
            labelTransform.rotation = camera.transform.rotation;

            lineRenderer.widthCurve = new AnimationCurve(
                                        new Keyframe(0.0f, 0.4f),
                                        new Keyframe(0.951f, 0.4f),
                                        new Keyframe(0.952f, 1.0f),
                                        new Keyframe(1.0f, 0.0f)
                                        );

            lineRenderer.SetPositions(new Vector3[] {
                transform.position,
                Vector3.Lerp(transform.position, transform.position + direction * length, 0.951f),
                Vector3.Lerp(transform.position, transform.position + direction * length, 0.952f),
                transform.position + direction * length
            });
        }
    }
}
