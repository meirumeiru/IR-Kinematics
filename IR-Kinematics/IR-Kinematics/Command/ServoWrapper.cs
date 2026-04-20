using UnityEngine;

using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Module;


namespace IR_Kinematics.Command
{
// FEHLER, hatte servo.transform und servo.part.transform drin -> das part braucht's doch nicht da dazwischen, oder? -> wenn gut, Kommentare raus

    public class ServoWrapper
	{
		public ModuleIRServo_v3 servo; // FEHLER, temp als ModuleIRServo_v3 genutzt, evtl. später als IServo nutzen versuchen

		private Solver.IIKServoWrapper iks;

		public ServoWrapper(IServo s)
		{
			servo = (ModuleIRServo_v3)s;

			iks = Controller.s.Create_IKServoWrapper(servo.part.persistentId, servo/*.part*/.transform.position, servo/*.part*/.transform.rotation, _GetAxis(), _GetSecAxis(), _GetAnchor(), servo.IsRotational, servo.HasMinMaxPosition || servo.IsLimited);
		}

		private Vector3 _GetAxis()
		{ return Quaternion.Inverse(servo.transform.rotation) * servo.GetAxis(); }

		private Vector3 _GetSecAxis()
		{ return Quaternion.Inverse(servo.transform.rotation) * servo.GetSecAxis(); }

		private Vector3 _GetAnchor()
		{ return Quaternion.Inverse(servo.transform.rotation) * (servo.GetAnchor() - servo.transform.position); }

		////////////////////////////////////////
		// IK solver data

		public Solver.IIKServoWrapper IKS
		{
			get { return iks; }
		}

		public void SetPointerPart(Part newPointerPart)
		{
			iks.SetPointerPart(newPointerPart.persistentId,
				Quaternion.Inverse(servo.transform.rotation) *
					(newPointerPart.transform.position - servo/*.part*/.transform.position));
		}

		public void SetPosition()
		{
			iks.SetPosition(servo.transform.position, servo.transform.rotation,
				servo.Position);
		}

		////////////////////////////////////////
		// IK limiter data

		public Vector3 endEffectorDistance;

		public Part largestDistancePart;
		public Vector3 largestDistance;
	}
}
