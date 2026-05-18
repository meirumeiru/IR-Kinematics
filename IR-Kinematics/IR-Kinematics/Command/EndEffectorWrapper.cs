using UnityEngine;


namespace IR_Kinematics.Command
{
	// not used at the moment -> future idea

	public class EndEffectorWrapper
	{
		public Part part;
		public Transform pEndEffectorTransform;

		private Solver.IIKEndEffectorWrapper ike;

		public EndEffectorWrapper(Part p, Transform p_endEffectorTransform, Vector3 p_localEndEffectorUp, Vector3 p_localEndEffectorRight)
		{
			part = p;
			pEndEffectorTransform = p_endEffectorTransform;

			ike = Controller.solver.Create_IKEndEffectorWrapper(part.persistentId,
				pEndEffectorTransform.position, pEndEffectorTransform.rotation, p_localEndEffectorUp, p_localEndEffectorRight);
		}

		public Vector3 Up
		{
			get { return ike.Up; }
		}

		public Vector3 Right
		{
			get { return ike.Right; }
		}

		public Vector3 Position
		{
			get { return pEndEffectorTransform.position; }
		}

		public Quaternion Rotation
		{
			get { return pEndEffectorTransform.rotation; }
		}

		////////////////////////////////////////
		// IK solver data

		public Solver.IIKEndEffectorWrapper IKE
		{
			get { return ike; }
		}

		public void UpdatePosition()
		{
			ike.UpdatePosition(pEndEffectorTransform.position, pEndEffectorTransform.rotation);
		}
	}
}
