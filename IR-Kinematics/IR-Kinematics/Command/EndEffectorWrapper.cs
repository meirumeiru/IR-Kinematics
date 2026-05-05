using UnityEngine;


namespace IR_Kinematics.Command
{
	// not used at the moment -> future idea

	public class EndEffectorWrapper
	{
		public Part part;
// FEHLER, 3 Teils sind neue Idee...
		public Transform pEndEffectorTransform;
		public Vector3 localEndEffectorUp;						// gibt an, was in der Gizmo-Anzeige als "up" gelten soll
		public Vector3 localEndEffectorRight;					// gibt an, was in der Gizmo-Anzeige als "right" gelten soll

		private Solver.IIKEndEffectorWrapper ike;

		public EndEffectorWrapper(Part p, Transform p_endEffectorTransform, Vector3 p_localEndEffectorUp, Vector3 p_localEndEffectorRight)
		{
			part = p;
			pEndEffectorTransform = p_endEffectorTransform;
			localEndEffectorUp = p_localEndEffectorUp;
			localEndEffectorRight = p_localEndEffectorRight;

			ike = Controller.s.Create_IKEndEffectorWrapper(part.persistentId,
				pEndEffectorTransform.position, pEndEffectorTransform.rotation, localEndEffectorUp, localEndEffectorRight);
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
