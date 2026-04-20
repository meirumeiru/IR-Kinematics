using UnityEngine;

#if DEBUG

namespace IR_Kinematics.Utility
{
	public class PlaneDrawer
	{
		private MeshFilter meshFilter;
		private MeshRenderer meshRenderer;

		public PlaneDrawer(Transform parent = null)
		{
			GameObject planeObj = new GameObject("PlaneObj");

			meshFilter = planeObj.AddComponent<MeshFilter>();
			meshRenderer = planeObj.AddComponent<MeshRenderer>();

			planeObj.transform.parent = parent;

			planeObj.transform.localPosition = Vector3.zero;
			planeObj.transform.localRotation = Quaternion.identity;
/*
			Mesh mesh = new Mesh();
			mesh.vertices = new Vector3[]
			{
				new Vector3(0, 0, 0),
				new Vector3(10, 0, 0),
				new Vector3(0, 10, 0),
				new Vector3(0, 10, 0),
				new Vector3(10, 0, 0),
				new Vector3(0, 0, 0)
			};
			mesh.triangles = new int[] { 0, 1, 2, 3, 4, 5 };
			mesh.RecalculateNormals();

			meshFilter.mesh = mesh;
*/
			Material material = new Material(Shader.Find("Hidden/Internal-Colored"));

			material.SetFloat("_Mode", 3);
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			material.SetInt("_ZWrite", 0);
			material.DisableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHABLEND_ON");
			material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

	//		material.color = new Color(255.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f, 0.7f);

			meshRenderer.material = material;

			meshRenderer.enabled = false;
		}

		public void Destroy()
		{
			if(meshFilter != null)
				UnityEngine.Object.Destroy(meshFilter.gameObject);
		}

		public void Reset(Transform parent = null)
		{
			meshFilter.gameObject.transform.parent = parent;
		}

		// draws the line through the provided vertices
		public void Draw(Vector3 center, Vector3 left, Vector3 right, Color color)
		{
			if(meshRenderer == null)
				return;

			center = meshFilter.gameObject.transform.InverseTransformPoint(center);
			left = meshFilter.gameObject.transform.InverseTransformVector(left);
			right = meshFilter.gameObject.transform.InverseTransformVector(right);

			Mesh mesh = new Mesh();
			mesh.vertices = new Vector3[]
			{
				center,
				center + left,
				center + right,
				center + right,
				center + left,
				center
			};
			mesh.triangles = new int[] { 0, 1, 2, 3, 4, 5 };
			mesh.RecalculateNormals();

			meshFilter.mesh = mesh;

			meshRenderer.material.color = color;

			meshRenderer.enabled = true;
		}

		// hides the line
		public void Hide()
		{
			meshRenderer.enabled = false;
		}
	}
}

#endif
