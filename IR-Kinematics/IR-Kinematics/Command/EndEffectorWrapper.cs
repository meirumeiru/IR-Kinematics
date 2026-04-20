using UnityEngine;


namespace IR_Kinematics.Command
{
	// not used at the moment -> future idea

	public class EndEffectorWrapper
	{
		public Part part;

		private Solver.IIKPartWrapper ikp;

		public EndEffectorWrapper(Part p)
		{
			part = p;

			ikp = Controller.s.Create_IKPartWrapper(part.persistentId, part.transform.position, part.transform.rotation);
		}

		////////////////////////////////////////
		// IK solver data

		public Solver.IIKPartWrapper IKP
		{
			get { return ikp; }
		}

		public void SetPosition()
		{
			ikp.SetPosition(part.transform.position, part.transform.rotation, 0);
		}
	}
}
