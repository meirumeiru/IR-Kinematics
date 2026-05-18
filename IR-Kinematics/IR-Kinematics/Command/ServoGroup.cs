using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Module;


namespace IR_Kinematics.Command
{
	public class ServoGroup
	{
		public static System.UInt32 id = 0;

	//	public static bool bUseIKG2 = false;

		public IServoGroup servoGroup;

		private Solver.IIKServoGroup ikg;
	//	public System.UInt32 ikg2_id = 0;

		public bool bIsValidGroup = false;						// gibt an, ob IK für diese Gruppe überhaupt möglich ist oder nicht


		public List<ServoWrapper> servos = null;                // all servos

		public List<ServoWrapper> ikServosForward = null;       // servos used for IK -> forwards (0 = basis)
		public List<ServoWrapper> ikServosBackward = null;      // servos used for IK -> backwards (0 = last servo before endeffector)


	//	public Part pEndEffectorPart;
	//	public Transform pEndEffectorTransform;
		public EndEffectorWrapper endEffector;

		public bool bShowPosition = false;


		public Vector3 targetPositionOffset;
		public Quaternion targetRotationOffset;


		public Part rootPart;
		public Vector3 localTargetPosition;						// targetPosition relativ zum rootPart
		public Quaternion localTargetRotation;					// targetRotation relativ zum rootPart

		public bool isReversed = false;


		////////////////////////////////////////
		// Constructor/Destructor

		public ServoGroup(IServoGroup p_servoGroup)
		{
			servoGroup = p_servoGroup;

			servos = new List<ServoWrapper>();

			foreach(IServo servo in servoGroup.Servos)
				servos.Add(new ServoWrapper(servo));
		}

		~ServoGroup()
		{
			UninitializeIK();		// FEHLER, das ist der EINZIGE Ort wo wir das aus der Gruppe aufrufen dürfen -> weil -> sonst bleibt was liegen im dümmsten Fall und das wollen wir nicht
		}

		////////////////////////////////////////
		// Compare / Helper

		public bool Compare(IServoGroup p_servoGroup)
		{
			if(p_servoGroup.Servos.Count != servos.Count)
				return false;

			for(int i = 0; i < servos.Count; i++)
			{
				if(servos[i].servo.servo != p_servoGroup.Servos[i])
					return false;
			}

			return true;
		}

		private ServoWrapper Find(Part p)
		{
			for(int i = 0; i < servos.Count; i++)
			{
				if(servos[i].servo.part == p)
					return servos[i];
			}

			return null;
		}
/*
		private bool isChildOf(Part part, Part potentialParentPart)
		{
			if(part.parent == null)
				return false;

			if(part.parent == potentialParentPart)
				return true;

			return isChildOf(part.parent, potentialParentPart);
		}
*/
		private int GetDepth(Part p)
		{
			int d = 0;
			while(p.parent != null)
			{ ++d; p = p.parent; }
			return d;
		}

		////////////////////////////////////////
		// Properties

		public string Name
		{
			get { return servoGroup.Name; }
		}

		public IList<IServo> Servos
		{
			get { return servoGroup.Servos; }
		}

		public Solver.IIKServoGroup IKG
		{
			get { return ikg; }
		}

// FEHLER, mal sehen wie wir das wieder reinbekommen
		public void Clear()
		{
	//		IKG.Clear();
		}

		////////////////////////////////////////
		// Command

		public void GetTargetPosition(out Vector3 currentTargetPosition, out Quaternion currentTargetRotation, bool bReal = false)
		{
			IKG.GetTargetPosition(out currentTargetPosition, out currentTargetRotation);

			if(!bReal)
			{
				currentTargetPosition += currentTargetRotation * targetPositionOffset;
				currentTargetRotation *= targetRotationOffset;
			}
		}

		public void SetTargetPosition(Vector3 newTargetPosition, Quaternion newTargetRotation, bool bReal = false)
		{
			if(!bReal)
			{
				newTargetRotation = newTargetRotation * Quaternion.Inverse(targetRotationOffset);
				newTargetPosition -= newTargetRotation * targetPositionOffset;
			}

			localTargetPosition = Quaternion.Inverse(rootPart.transform.rotation) * (newTargetPosition - rootPart.transform.position);
			localTargetRotation = Quaternion.Inverse(rootPart.transform.rotation) * newTargetRotation;

			IKG.SetTargetPosition(newTargetPosition, newTargetRotation);
		}

		public void ResetTargetPosition()
		{
			SetTargetPosition(endEffector.Position, endEffector.Rotation, true);
		}

		////////////////////////////////////////
		// Update

		// aktualisiert die targetPosition/targetRotation anhand vom vessel und dem localTargetPosition/localTargetRotation
		public void FixedUpdate()
		{
			IKG.SetTargetPosition(
				rootPart.transform.position + rootPart.transform.rotation * localTargetPosition,
				rootPart.transform.rotation * localTargetRotation);
		}

		////////////////////////////////////////
		// End Effector

		public static ServoGroup pSetEndEffectorGroup;   // dieser Gruppe setzen wir gerade den EndEffector

		public bool StartSelectEndEffector()
		{
			if(pSetEndEffectorGroup != null)
				return false;

			pSetEndEffectorGroup = this;

			return true;
		}
/*
		private bool IsUsableEndEffector(Part p)
		{
			while(p)
			{
				if(Find(p) != null)
					return true;

				p = p.parent;
			}

			return false;
		}
*/
		public bool EndSetEndEffector(Part p)
		{
			if(p == null)
				pSetEndEffectorGroup = null;
			else
			{
				if(p.vessel != ikServosBackward[0].servo.vessel)
					return false;

				pSetEndEffectorGroup = null;

				SetEndEffector(p);
			}

			return true;
		}

		private Part SearchEndEffector(Part part)
		{
			foreach(Part child in part.children)
			{
				if(child.FindModuleImplementing<DockingFunctions.IDockable>() != null)
					return child;
			}

			foreach(Part child in part.children)
			{
				Part result = SearchEndEffector(child);
				
				if(result != null)
					return result;
			}

			return null;
		}

		public void AutoSelectEndeffector()
		{
			Part p = ikServosBackward[0].servo.HostPart;

// FEHLER, neue Idee
Part efp = SearchEndEffector(p);
if(efp != null)
	p = efp;
else if(p.children.Count > 0)
	p = p.children[0];

			SetEndEffector(p);
		}

		private void SetEndEffector(Part pEndEffectorPart)
		{
// FEHLER, die Gruppe könnte "invalid" sein nach dem hier -> evtl. müsste man hier also exceptions abfangen? oder sonstwie bei Init-Funktionen, damit es nicht kracht deswegen?

			DockingFunctions.IDockable port = pEndEffectorPart.GetComponent<DockingFunctions.IDockable>();

			Vector3 Up, Right; Transform pEndEffectorTransform;

			if(port != null)
			{
				pEndEffectorTransform = port.GetNodeTransform();

				Up = Vector3.forward;
				Right = port.GetDockingOrientation();
			}
			else
			{
				pEndEffectorTransform = pEndEffectorPart.transform;

				Up = Vector3.up;
				Right = Vector3.right;
			}

			endEffector = new EndEffectorWrapper(pEndEffectorPart, pEndEffectorTransform, Up, Right);

			targetPositionOffset = Vector3.zero;
			targetRotationOffset = Quaternion.identity;


// FEHLER, prüfen, ob's verdreht ist, wenn ja, etwas in die Wege leiten...
			switch(EndEffectorPosition())
			{
			case 1:
				isReversed = false;
				rootPart = ikServosForward[0].servo.HostPart.parent;
				break;

			case -1:
				isReversed = true;
				rootPart = ikServosBackward[0].servo.HostPart;
				break;
			}


			localTargetPosition = Quaternion.Inverse(rootPart.transform.rotation) * (pEndEffectorTransform.position - rootPart.transform.position);
			localTargetRotation = Quaternion.Inverse(rootPart.transform.rotation) * pEndEffectorTransform.rotation;
		}

// FEHLER, lösen wir mal das EndEffector-Problem

		public static bool FindElementInChildren(Part part, Part searchedPart)
		{
			if(part == searchedPart)
				return true;

			for(int i = 0; i < part.children.Count; i++)
			{
				if(FindElementInChildren(part.children[i], searchedPart))
					return true;
			}

			return false;
		}

		public static bool FindElementInParent(Part part, Part searchedPart)
		{
			if(part == searchedPart)
				return true;

			if(part.parent == null)
				return false;

			for(int i = 0; i < part.parent.children.Count; i++)
			{
				if((part.parent.children[i] != part) && FindElementInChildren(part.parent.children[i], searchedPart))
					return true;
			}

			return FindElementInParent(part.parent, searchedPart);
		}

		public int EndEffectorPosition()
		{
			if(ikServosBackward[0].servo.vessel != endEffector.part.vessel)
				return 0; // not part of same vessel // FEHLER, in Zukunft auch Latch-Verbindungen unterstützen -> braucht dann halt noch mehr Info und Interface

			if(FindElementInChildren(ikServosBackward[0].servo.HostPart, endEffector.part))
				return 1; // in child

			if(FindElementInParent(ikServosForward[0].servo.HostPart, endEffector.part))
				return -1; // in parent

			return 0; // FEHLER, sollte nie passieren eigentlich...
		}

		public void GetTargetOffset(out Vector3 currentTargetPositionOffset, out Quaternion currentTargetRotationOffset)
		{
			currentTargetPositionOffset = targetPositionOffset;
			currentTargetRotationOffset = targetRotationOffset;
		}

		public void SetTargetOffset(Vector3 newTargetPositionOffset, Quaternion newTargetRotationOffset)
		{
			targetPositionOffset = newTargetPositionOffset;
			targetRotationOffset = newTargetRotationOffset;
		}

		////////////////////////////////////////
		// Build IK Group

float fReversed = 1f;

struct scheisse : System.IComparable<scheisse> {

public scheisse(Part _p, int _d) { p = _p; d = _d; }

public Part p; public int d;

public int CompareTo(scheisse other)
{
	return d - other.d;
}
}

		// wird aufgerufen, direkt nachdem wir die ServoGroup bauen (was soviel heisst wie -> vom IR übernehmen)
		public void InitializeIK()
		{
			if(servos.Count == 0)
				return; // FEHLER, eigentlich dürfte so ein Aufruf gar nicht kommen -> ist ja sinnlos

			// remarks: all available servos are in 'servos'
			// build the sorted lists of available servos

			ikServosForward = new List<ServoWrapper>();

// FEHLER, alles neu, alles prüfen... shit nochmal

			List<scheisse> pE = new List<scheisse>();

			foreach(InfernalRobotics_v3.Interfaces.IServo s in servoGroup.Servos)
			{
				pE.Add(new scheisse( s.servo.HostPart, GetDepth(s.servo.HostPart) ));
			}

			pE.Sort();

			foreach(var p in pE)
			{
				ServoWrapper sw = Find(p.p);
				if(sw != null)
					ikServosForward.Add(sw);
			}

			ikServosBackward = new List<ServoWrapper>(ikServosForward);
			ikServosBackward.Reverse();


// FEHLER, temp, Quickfix, wobei, keine echte Lösung
if((endEffector == null) || !endEffector.part/*|| (EndEffectorPosition() != 1)*/)
	AutoSelectEndeffector();

		//	SetEndEffector();


			Controller.solver.InitializeIK(isReversed, ikServosForward, ikServosBackward,
				endEffector, rootPart,
				out ikg, out bIsValidGroup);

//pEndEffectorWrapper übergeben; // FEHLER, ich probier umzustellen

			if(!bIsValidGroup)
				ScreenMessages.PostScreenMessage("invalid group", 5, ScreenMessageStyle.UPPER_CENTER);

/*
			Solver.IKServoWrapper[] ikServos;
			
			if(!isReversed)
			{
				ikServos = new Solver.IKServoWrapper[ikServosForward.Count];
				for(int i = 0; i < ikServosForward.Count; i++)
					ikServos[i] = (Solver.IKServoWrapper)ikServosForward[i].IKS;
			}
			else
			{
				ikServos = new Solver.IKServoWrapper[ikServosBackward.Count];
				for(int i = 0; i < ikServosBackward.Count; i++)
					ikServos[i] = (Solver.IKServoWrapper)ikServosBackward[i].IKS;
			}

			if(rv0)
				fReversed = isReversed ? -1f : 1f;

			if(rv2)
			{
				for(int i = 0; i < ikServos.Length; i++)
					ikServos[i].SetInverted(isReversed);
			}

			Solver.IKServoGroup _ikg = new Solver.IKServoGroup(ikServos, isReversed);
			ikg = _ikg;


			_ikg.Initialize(pEndEffectorTransform.position, pEndEffectorTransform.rotation);

// FEHLER
// >> Anfang Versuch

//if(ServoGroup.bUseIKG2)
//{
//			System.IO.MemoryStream ms = new System.IO.MemoryStream();

//			System.IO.BinaryWriter bw = new System.IO.BinaryWriter(ms, System.Text.Encoding.Unicode, true);
//			ikg.SerializeInfo(bw);
//			bw.Close();

//			if(ikg2_id != 0)
//				Solver.IKSolverxtern.DeleteGroup(ikg2_id);
//			ikg2_id = ++id;
//			Solver.IKSolverxtern.CreateGroup(ikg2_id, ms);

//			ms.Close();
//}

// << Ende Versuch

			bIsValidGroup = _ikg.IsValid();

#if DEBUG
			if(bIsValidGroup)
				_ikg.InitializeDebug(rootPart);
#endif
*/
		}

		public void UninitializeIK()
		{
/*
#if DEBUG
			((Solver.IKServoGroup)IKG).UninitializeDebug();
#endif
*/

			Controller.solver.UninitializeIK(IKG);

//			if(ikg2_id != 0)
//				Solver.IKSolverxtern.DeleteGroup(ikg2_id);
//			ikg2_id = 0;
		}

		// sets or resets all the positions of the servos and the endeffector to current values
		// this is needed before every new calculation run
		public void UpdateIK()
		{
			for(int i = 0; i < servos.Count; i++)
				servos[i].UpdatePosition();

			endEffector.UpdatePosition();

			IKG.SetTargetPosition(
				rootPart.transform.position + rootPart.transform.rotation * localTargetPosition,
				rootPart.transform.rotation * localTargetRotation);
		}

		////////////////////////////////////////
		// Move

		public void Stop()
		{
			if(servos.Any())
			{
				foreach(var servo in servoGroup.Servos)
					servo.Stop();
			}
		}

// FEHLER, keine Ahnung, ob das auch nur halbwegs stimmt... aber, kann man ja mal ausprobieren ob's passt...
/*		public void Predict(float factor, out Vector3 position, out Quaternion rotation, int count)
		{
			Vector3 pos = ikServosForward[0].IKS.Position;
			Quaternion rot = Quaternion.identity;

			for(int i = 0; i < ikServosForward.Count; i++)
			{
				Quaternion _localRot = Quaternion.identity;

				if(i > 0)
					_localRot = Quaternion.Inverse(ikServosForward[i - 1].servo.transform.rotation);
				_localRot = _localRot * ikServosForward[i].servo.transform.rotation;


				float relRotCommand = fReversed * ikServosForward[i].IKS.TotalRelRotCommand;
				Quaternion _rotation = Quaternion.AngleAxis(relRotCommand * factor, ikServosForward[i].IKS.GetAxis());


				// FEHLER, erste Idee -> nur die ersten count Servos überhaupt drehen
				if(i >= count)
					_rotation = Quaternion.identity;

				rot = rot * _localRot * _rotation;

				if(i+1 < ikServosForward.Count)
				{
					Vector3 relPos = ikServosForward[i].servo.transform.InverseTransformPoint(
						ikServosForward[i + 1].servo.transform.position);

					pos = pos + rot * relPos;
				}
			}

			// ausgeben wo der Pointer hinzeigt...

			position = pos
				- rot * Quaternion.Inverse(IKG.endEffectorRotationToTargetRotation)
					* this.IKG.endEffectorPositionToTargetPosition;

			rotation = rot * Quaternion.Inverse(IKG.endEffectorRotationToTargetRotation);
		}*/

		public void Execute(float maxDistance, float accelerationFactor)
		{
			// FEHLER, Limiter alleine reicht nicht, weil -> klar, normal will ich nur limitieren, aber hier will ich noch, dass alles gleichzeitig aufhört, daher muss ich's anders machen beim Execute

			CalculateLimits();

			float fFactor = 1f; float fMaxDist = maxDistance; // und das ist schon viel

			for(int i = 0; i < ikServosForward.Count; i++)
			{
				if((ikServosForward[i].Speed * ikServosForward[i].endEffectorDistance).magnitude > fMaxDist)
				{
					float fact = fMaxDist / (ikServosForward[i].Speed * ikServosForward[i].endEffectorDistance).magnitude;

					fFactor = Mathf.Min(fFactor, fact);
				}
			}

			for(int i = 0; i < ikServosForward.Count; i++)
			{
				float relRotCommand = ikServosForward[i].servo.IsReversed ? -ikServosForward[i].TotalRelRotCommand : ikServosForward[i].TotalRelRotCommand;
				float speed = ikServosForward[i].Speed * fFactor;

				ikServosForward[i].servo.PrecisionMove(fReversed * relRotCommand, speed, speed * accelerationFactor, true);
			}
		}

		////////////////////////////////////////
		// Limiter

		void CalculateLimit(ServoWrapper s)
		{
			s.endEffectorDistance = endEffector.Position - s.servo.transform.position;

			s.endEffectorDistance =
				Vector3.ProjectOnPlane(s.endEffectorDistance, s.GetAxisGlobal());

			// largestDistance -> FEHLER, später vielleicht mal... nicht jetzt
		}

		void CalculateLimits()
		{
			for(int i = 0; i < ikServosForward.Count; i++)
				CalculateLimit(ikServosForward[i]);
		}
	}
}
