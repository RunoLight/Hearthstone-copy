﻿using UnityEngine;
using System.Collections;
using TMPro;

public class WarpText : MonoBehaviour
{
    private TMP_Text mTextComponent;

    public AnimationCurve vertexCurve = new AnimationCurve(
        new Keyframe(0, 0), new Keyframe(0.25f, 2.0f), new Keyframe(0.5f, 0),
        new Keyframe(0.75f, 2.0f), new Keyframe(1, 0f)
    );

    public float curveScale = 1.0f;

    private void Awake()
    {
        mTextComponent = gameObject.GetComponent<TMP_Text>();
    }

    private void Start()
    {
        StartCoroutine(WarpTextMesh());
    }

    private static AnimationCurve CopyAnimationCurve(AnimationCurve curve)
    {
        var newCurve = new AnimationCurve
        {
            keys = curve.keys
        };

        return newCurve;
    }

    /// <summary>
    ///  Method to curve text along a Unity animation curve.
    /// </summary>
    /// <returns></returns>
    private IEnumerator WarpTextMesh()
    {
        vertexCurve.preWrapMode = WrapMode.Clamp;
        vertexCurve.postWrapMode = WrapMode.Clamp;

        Vector3[] vertices;
        Matrix4x4 matrix;

        // Need to force the TextMeshPro Object to be updated.
        mTextComponent.havePropertiesChanged = true; 
        float oldCurveScale = curveScale;
        AnimationCurve oldCurve = CopyAnimationCurve(vertexCurve);

        while (true)
        {
            if (!mTextComponent.havePropertiesChanged && oldCurve.keys[1].value == vertexCurve.keys[1].value)
            {
                yield return null;
                continue;
            }

            oldCurveScale = curveScale;
            oldCurve = CopyAnimationCurve(vertexCurve);

            // Generate the mesh and populate the textInfo with data we can use and manipulate.
            mTextComponent.ForceMeshUpdate(); 

            TMP_TextInfo textInfo = mTextComponent.textInfo;
            int characterCount = textInfo.characterCount;
            
            if (characterCount == 0) continue;

            float boundsMinX = mTextComponent.bounds.min.x; //textInfo.meshInfo[0].mesh.bounds.min.x;
            float boundsMaxX = mTextComponent.bounds.max.x; //textInfo.meshInfo[0].mesh.bounds.max.x;


            for (int i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                // Get the index of the mesh used by this character.
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                vertices = textInfo.meshInfo[materialIndex].vertices;

                // Compute the baseline mid point for each character
                Vector3 offsetToMidBaseline =
                    new Vector2((vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2,
                                textInfo.characterInfo[i].baseLine);
                //float offsetY = VertexCurve.Evaluate((float)i / characterCount + loopCount / 50f); // Random.Range(-0.25f, 0.25f);

                // Apply offset to adjust our pivot point.
                vertices[vertexIndex + 0] += -offsetToMidBaseline;
                vertices[vertexIndex + 1] += -offsetToMidBaseline;
                vertices[vertexIndex + 2] += -offsetToMidBaseline;
                vertices[vertexIndex + 3] += -offsetToMidBaseline;

                // Compute the angle of rotation for each character based on the animation curve
                float x0 = (offsetToMidBaseline.x - boundsMinX) /
                           (boundsMaxX - boundsMinX); // Character's position relative to the bounds of the mesh.
                float x1 = x0 + 0.0001f;
                float y0 = vertexCurve.Evaluate(x0) * curveScale;
                float y1 = vertexCurve.Evaluate(x1) * curveScale;

                Vector3 horizontal = new Vector3(1, 0, 0);
                //Vector3 normal = new Vector3(-(y1 - y0), (x1 * (boundsMaxX - boundsMinX) + boundsMinX) - offsetToMidBaseline.x, 0);
                Vector3 tangent = new Vector3(x1 * (boundsMaxX - boundsMinX) + boundsMinX, y1) -
                                  new Vector3(offsetToMidBaseline.x, y0);

                float dot = Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * 57.2957795f;
                Vector3 cross = Vector3.Cross(horizontal, tangent);
                float angle = cross.z > 0 ? dot : 360 - dot;

                matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

                vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                vertices[vertexIndex + 0] += offsetToMidBaseline;
                vertices[vertexIndex + 1] += offsetToMidBaseline;
                vertices[vertexIndex + 2] += offsetToMidBaseline;
                vertices[vertexIndex + 3] += offsetToMidBaseline;
            }

            // Upload the mesh with the revised information
            mTextComponent.UpdateVertexData();

            yield return new WaitForSeconds(0.025f);
        }
    }
}