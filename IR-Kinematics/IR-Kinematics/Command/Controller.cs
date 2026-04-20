using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace IR_Kinematics.Command
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class Controller : MonoBehaviour, InfernalRobotics_v3.Interfaces.IIKModule
	{
		public virtual String AddonName { get; set; }

		protected static Controller ControllerInstance;

		public static Controller Instance { get { return ControllerInstance; } }

		public static bool APIReady { get { return ControllerInstance != null; } }

		public static Solver.IIKSolver s;
		public Limiter.Limiter lm;

		////////////////////////////////////////
		// Data

		public ServoGroup pActiveGroup = null; // currently active group for IK

		public GameObject targetPointer;
		public GameObject positionPointer;

		bool bDirectMode = false;
		bool bLimiter = false;

		public List<ServoGroup> aAllGroups = new List<ServoGroup>();

		////////////////////////////////////////
		// Callbacks

		public void Start()
		{
			ServoGroup.pSetEndEffectorGroup = null;
		}

		private void Awake()
		{
			if(!HighLogic.LoadedSceneIsFlight)
			{
				ControllerInstance = null;
				return;
			}

			ControllerInstance = this;
			InfernalRobotics_v3.Command.Controller.RegisterIKModule(this);

		//	s = new Solver.IKZeugs();
			lm = new Limiter.Limiter();
		}

		private void OnDestroy()
		{
			InfernalRobotics_v3.Command.Controller.RegisterIKModule(null);
		}

		////////////////////////////////////////
		// Helper

		private ServoGroup GetGroup(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			for(int i = 0; i < aAllGroups.Count; i++)
			{
				if(aAllGroups[i].servoGroup == g)
					return aAllGroups[i];
			}

			return null;
		}

		private ServoGroup QueryGroup(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			int i = 0;
			while((i < aAllGroups.Count) && (aAllGroups[i].servoGroup != g))
				++i;

			if(i < aAllGroups.Count)
			{
				if(aAllGroups[i].Compare(g))
				{
//aAllGroups[i].vessel = g.Vessel; // FEHLER, Bugfix, weil, der kann ändern -> und: sicherstellen, dass wir überhaupt hierher kommen, wenn das passiert!!!

//	das reicht nicht -> entweder immer von Gruppe auslesen oder eine Update-Funktion erfinden... shit
//	und das mit dem blöden endeffector-verify ist ja auch noch nicht korrekt... shit noch eins ... ausser 's läge am gleichen?
		// -> neu setzen wir von aussen einfach die Gruppe dann immer neu? ist das die "Lösung" ???

aAllGroups[i].UpdateIK(); // FEHLER, Bugfix, nötig, weil sonst geht init nicht -> das prüfen, ob das überall stimmt
					aAllGroups[i].InitializeIK(); // re-init ist nötig, wenn wir das Zeug z.B. entfalten oder so
	
					return aAllGroups[i];
				}

				aAllGroups.RemoveAt(i);
			}

			ServoGroup sg = new ServoGroup(g);
			sg.InitializeIK();

			aAllGroups.Add(sg);

			return sg;
		}

		private static Part GetPartUnderCursor()
		{
			Ray ray;
			if(HighLogic.LoadedSceneIsFlight)
				ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
			else
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, 1000, 557059))
				return hit.transform.gameObject.GetComponent<Part>();
			else
				return null;
		}

//static int layer = 13; // 13 UI_Mask oder 23 AeroFX Ignore

		private bool NeptuneCameraTried = false;
		private Type ModuleNeptuneCameraType = null;
		private System.Reflection.MethodInfo ModuleNeptuneCameraSetPartProcessingMethod = null;
  
		private void PrepareNeptuneCamera()
		{
			NeptuneCameraTried = true;

			AssemblyLoader.loadedAssemblies.TypeOperation (t => { if(t.FullName == "NeptuneCameraNext.ModuleNeptuneCamera") { ModuleNeptuneCameraType = t; } });

			if(ModuleNeptuneCameraType != null)
				ModuleNeptuneCameraSetPartProcessingMethod = ModuleNeptuneCameraType.GetMethod("SetPartProcessing");
		}

		private void BuildPointer(out GameObject pointer, Color colorForward, Color colorRight, Color colorUp)
		{
			pointer = new GameObject();

			GameObject arrow;
			MeshRenderer[] rds;

			// forward arrow
			arrow = GameObject.Instantiate(Gui.UIAssetsLoader.gizmo);
			arrow.transform.parent = pointer.transform;
			arrow.transform.localPosition = Vector3.forward * 0.05f;
			arrow.transform.localRotation = Quaternion.LookRotation(Vector3.forward);
			arrow.transform.localScale = new Vector3(8f, 8f, 25f);
//			arrow.SetLayerRecursive(layer);

			rds = arrow.GetComponentsInChildren<MeshRenderer>();

			for(int i = 0; i < rds.Length; i++)
				rds[i].material.color = colorForward;

			// right arrow
			arrow = GameObject.Instantiate(Gui.UIAssetsLoader.gizmo);
			arrow.transform.parent = pointer.transform;
			arrow.transform.localPosition = Vector3.right * 0.05f;
			arrow.transform.localRotation = Quaternion.LookRotation(Vector3.right);
			arrow.transform.localScale = new Vector3(8f, 8f, 25f);
//			arrow.SetLayerRecursive(layer);

			rds = arrow.GetComponentsInChildren<MeshRenderer>();

			for(int i = 0; i < rds.Length; i++)
				rds[i].material.color = colorRight;

			// up arrow
			arrow = GameObject.Instantiate(Gui.UIAssetsLoader.gizmo);
			arrow.transform.parent = pointer.transform;
			arrow.transform.localPosition = Vector3.up * 0.05f;
			arrow.transform.localRotation = Quaternion.LookRotation(Vector3.up);
			arrow.transform.localScale = new Vector3(8f, 8f, 25f);
//			arrow.SetLayerRecursive(layer);

			rds = arrow.GetComponentsInChildren<MeshRenderer>();

			for(int i = 0; i < rds.Length; i++)
				rds[i].material.color = colorUp;

// Neptune-Camera informieren... // FEHLER, temp, erste Idee
			if(!NeptuneCameraTried)
				PrepareNeptuneCamera();

			if(ModuleNeptuneCameraSetPartProcessingMethod != null)
				ModuleNeptuneCameraSetPartProcessingMethod.Invoke(null, new object[] { pointer, true });
		}

		private void DestroyPointer(ref GameObject pointer)
		{
			if(ModuleNeptuneCameraSetPartProcessingMethod != null)
				ModuleNeptuneCameraSetPartProcessingMethod.Invoke(null, new object[] { pointer, false });

			int i = pointer.transform.childCount;
			while(i-- > 0)
				pointer.transform.GetChild(i).gameObject.DestroyGameObject();
			pointer.DestroyGameObject();
		}

		private void UpdatePointerPosition()
		{
// FEHLER, hab die targetRotationOffset Sache VOR das LookRotation genommen... vorher war's hinterher -> mal sehen ob das 'ne gute Idee war

			Vector3 targetPosition; Quaternion targetRotation;
			pActiveGroup.GetTargetPosition(out targetPosition, out targetRotation, true);

			targetPointer.transform.position = targetPosition + targetRotation * pActiveGroup.targetPositionOffset;
			targetPointer.transform.rotation = targetRotation
				* pActiveGroup.targetRotationOffset
				* Quaternion.LookRotation(pActiveGroup.localEndEffectorUp, pActiveGroup.localEndEffectorRight);

			if(pActiveGroup.bShowPosition)
			{
				positionPointer.transform.position = pActiveGroup.pEndEffectorTransform.position + pActiveGroup.pEndEffectorTransform.rotation * pActiveGroup.targetPositionOffset;
				positionPointer.transform.rotation = pActiveGroup.pEndEffectorTransform.rotation
					* pActiveGroup.targetRotationOffset
					* Quaternion.LookRotation(pActiveGroup.localEndEffectorUp, pActiveGroup.localEndEffectorRight);
			}
		}

		////////////////////////////////////////
		// Update-Functions

int Snc(int a, int b)
{
	if(a == b)
		return a;

	if(a < b)
	{
		int c = b / a;
		if(c * a == b)
			return a;
		return 1;
	}
	else
	{
		int c = a / b;
		if(c * b == a)
			return b;
		return 1;
	}
}

		private void FixedUpdate()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(pActiveGroup != null)
					pActiveGroup.FixedUpdate();

				ProcessInput();
			}
		}

static float maxMovement = 0.05f;
static float maxTurning = 2f;

		byte key_l = 0;
		byte key_j = 0;
		byte key_i = 0;
		byte key_k = 0;
		byte key_h = 0;
		byte key_n = 0;

		byte key_w = 0;
		byte key_s = 0;
		byte key_a = 0;
		byte key_d = 0;
		byte key_q = 0;
		byte key_e = 0;

		byte key_resetTarget = 0; byte key_resetOffset = 0;

		private void ResetKeys()
		{
			key_l = 0; key_j = 0; key_i = 0; key_k = 0; key_h = 0; key_n = 0;
			key_w = 0; key_s = 0; key_a = 0; key_d = 0; key_q = 0; key_e = 0;

			key_resetTarget = 0; key_resetOffset = 0;
		}

		private void CollectKeysDownUp(byte DownType, byte UpType)
		{
			if(Input.GetKey(KeyCode.LeftAlt) | Input.GetKey(KeyCode.RightAlt))
			{
				if(Input.GetKeyDown(KeyCode.K))
				{
					if(!(Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift)))
						key_resetTarget += 1;
					else
						key_resetOffset += 1;
				}
			}
			else
			{
				key_l += (byte)((Input.GetKeyDown(KeyCode.L) ? DownType : 0) + (Input.GetKeyUp(KeyCode.L) ? UpType : 0));
				key_j += (byte)((Input.GetKeyDown(KeyCode.J) ? DownType : 0) + (Input.GetKeyUp(KeyCode.J) ? UpType : 0));
				key_i += (byte)((Input.GetKeyDown(KeyCode.I) ? DownType : 0) + (Input.GetKeyUp(KeyCode.I) ? UpType : 0));
				key_k += (byte)((Input.GetKeyDown(KeyCode.K) ? DownType : 0) + (Input.GetKeyUp(KeyCode.K) ? UpType : 0));
				key_h += (byte)((Input.GetKeyDown(KeyCode.H) ? DownType : 0) + (Input.GetKeyUp(KeyCode.H) ? UpType : 0));
				key_n += (byte)((Input.GetKeyDown(KeyCode.N) ? DownType : 0) + (Input.GetKeyUp(KeyCode.N) ? UpType : 0));

				key_w += (byte)((Input.GetKeyDown(KeyCode.W) ? DownType : 0) + (Input.GetKeyUp(KeyCode.W) ? UpType : 0));
				key_s += (byte)((Input.GetKeyDown(KeyCode.S) ? DownType : 0) + (Input.GetKeyUp(KeyCode.S) ? UpType : 0));
				key_a += (byte)((Input.GetKeyDown(KeyCode.A) ? DownType : 0) + (Input.GetKeyUp(KeyCode.A) ? UpType : 0));
				key_d += (byte)((Input.GetKeyDown(KeyCode.D) ? DownType : 0) + (Input.GetKeyUp(KeyCode.D) ? UpType : 0));
				key_q += (byte)((Input.GetKeyDown(KeyCode.Q) ? DownType : 0) + (Input.GetKeyUp(KeyCode.Q) ? UpType : 0));
				key_e += (byte)((Input.GetKeyDown(KeyCode.E) ? DownType : 0) + (Input.GetKeyUp(KeyCode.E) ? UpType : 0));
			}
		}

		private void CollectKeys(byte Type)
		{
			if(Input.GetKey(KeyCode.LeftAlt) | Input.GetKey(KeyCode.RightAlt))
				return;

			key_l += (byte)(Input.GetKey(KeyCode.L) ? Type : 0);
			key_j += (byte)(Input.GetKey(KeyCode.J) ? Type : 0);
			key_i += (byte)(Input.GetKey(KeyCode.I) ? Type : 0);
			key_k += (byte)(Input.GetKey(KeyCode.K) ? Type : 0);
			key_h += (byte)(Input.GetKey(KeyCode.H) ? Type : 0);
			key_n += (byte)(Input.GetKey(KeyCode.N) ? Type : 0);

			key_w += (byte)(Input.GetKey(KeyCode.W) ? Type : 0);
			key_s += (byte)(Input.GetKey(KeyCode.S) ? Type : 0);
			key_a += (byte)(Input.GetKey(KeyCode.A) ? Type : 0);
			key_d += (byte)(Input.GetKey(KeyCode.D) ? Type : 0);
			key_q += (byte)(Input.GetKey(KeyCode.Q) ? Type : 0);
			key_e += (byte)(Input.GetKey(KeyCode.E) ? Type : 0);
		}

		private void SetTarget(Part targetPart)
		{
			DockingFunctions.IDockable port = targetPart.GetComponent<DockingFunctions.IDockable>();

			if(port != null)
			{
				Quaternion targetRotation =
					Quaternion.LookRotation(-port.GetNodeTransform().forward, port.GetNodeTransform().rotation * port.GetDockingOrientation())
					* Quaternion.Inverse(Quaternion.LookRotation(pActiveGroup.localEndEffectorUp, pActiveGroup.localEndEffectorRight));

				Vector3 targetPosition = port.GetNodeTransform().position;

// FEHLER, neue Idee -> wenn mehrere SnapAngle möglich sind, dann sollte man den nächsten wählen, wenn
// man sehr nahe drauf ist... damit man neu ausrichten kann, wenn man gleich davor steht

if((targetPosition - pActiveGroup.pEndEffectorTransform.position).magnitude < 0.5f) // FEHLER, unklar wie weit weg
{
	DockingFunctions.IDockable p = pActiveGroup.pEndEffectorPart.GetComponent<DockingFunctions.IDockable>();

	if(p != null)
	{
		int s = Snc(p.GetSnapCount(), port.GetSnapCount());
		float br = 360f / s;

		Quaternion tgt = targetRotation;

		for(int _s = 1; _s < s; _s++)
		{
			Quaternion tryRotation =
Quaternion.AngleAxis(_s * br, port.GetNodeTransform().forward) *
				Quaternion.LookRotation(-port.GetNodeTransform().forward, port.GetNodeTransform().rotation * port.GetDockingOrientation())
				* Quaternion.Inverse(Quaternion.LookRotation(pActiveGroup.localEndEffectorUp, pActiveGroup.localEndEffectorRight));

// FEHLER, eigentlich müsste man den Winkel betreffend der Achse nehmen, nicht einfach so... aber als erster Versuch ist das vielleicht mal was
			if(Quaternion.Angle(tgt, pActiveGroup.pEndEffectorTransform.rotation)
				> Quaternion.Angle(tryRotation, pActiveGroup.pEndEffectorTransform.rotation))
				tgt = tryRotation;
		}

		targetRotation = tgt;
	}
}

				pActiveGroup.SetTargetPosition(targetPosition, targetRotation, true);
			}
			else
			{
				Quaternion targetRotation =
					Quaternion.LookRotation(targetPart.transform.forward, -targetPart.transform.up)
					* Quaternion.Inverse(Quaternion.LookRotation(pActiveGroup.localEndEffectorUp, pActiveGroup.localEndEffectorRight));

				Vector3 targetPosition = port.GetNodeTransform().position;

				pActiveGroup.SetTargetPosition(targetPosition, targetRotation, true);
			}
		}

		Part endEffectorCoroutinePart;
		Coroutine endEffectorCoroutine;
		IEnumerator makeout(Part p)
		{
			yield return new WaitForSeconds(2);

			endEffectorCoroutinePart = null;
			endEffectorCoroutine = null;

			p.SetHighlightDefault();
		}

		public void Update()
		{
		//	if(InputLockManager.IsUnlocked(ControlTypes.LINEAR))
			{
				CollectKeysDownUp(
					(byte)((Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.RightControl)) ? 2 : 1),
					0);
			}

			// FEHLER, viele von dem Scheiss direkt die ControlGroup machen lassen... die aktive... einfach der den Scheiss weiterleiten... also echt jetzt

			if(ServoGroup.pSetEndEffectorGroup != null)
			{
				if(Input.GetKeyDown(KeyCode.Mouse0))
				{
					if(ServoGroup.pSetEndEffectorGroup.pEndEffectorPart)
						ServoGroup.pSetEndEffectorGroup.pEndEffectorPart.SetHighlightDefault();

					Part p = GetPartUnderCursor();
					if(p && OnEndEffectorSelected(p))
					{
						p.SetHighlight(true, false);
						p.SetHighlightColor(new Color(1.0f, 0.5f, 0.0f));
						p.SetHighlightType(Part.HighlightType.AlwaysOn);

						endEffectorCoroutinePart = p;
						endEffectorCoroutine = StartCoroutine(makeout(p));
					}

					Mouse.Left.ClearMouseState();
				}
			}

			if(bSelectTarget)
			{
				if(Input.GetKeyDown(KeyCode.Mouse0))
				{
					Part p = GetPartUnderCursor();
					if(p)
						SetTarget(p);

					bSelectTarget = false;
				}
			}
		}

		static float maxDistance = 1f;
		static float speedFactor = 5f;

		float fastCounter = 1f;
		int lastMode = 1;

		void ProcessInput()
		{
			if(pActiveGroup == null)
			{
				ResetKeys();
				return;
			}

			int currentMode = (Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift)) ? 0 : 1;

			if(currentMode != lastMode)
			{
				fastCounter = 1f;
				lastMode = currentMode;
				return;
			}

			byte DownType, PressDownType;

			if(Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.RightControl))
			{
				DownType = 2;

				fastCounter += Time.fixedDeltaTime;

				if(fastCounter < 1f)
					PressDownType = 0;
				else
				{
					PressDownType = 2;
					fastCounter = 0f;
				}
			}
			else
			{
				DownType = 1;
				fastCounter = 1f;
				PressDownType = 1;
			}

			CollectKeys(PressDownType);

			bool bFast = (DownType > 1);

			bool bUserInput = false;

			// FEHLER, Versuch -> ich probiere mal das aktuelle Teil anzuzeigen... seine Pfeile sozusagen
			// und die dann herumzuschieben... und das später als Ziel nutzen...
			if((pActiveGroup != null) && (pActiveGroup.pEndEffectorPart != null))
			{
				if(bLimiter)
				{
					Controller.Instance.lm.maxDistance = (pActiveGroup.servoGroup.GroupSpeedFactor / 2f) * maxDistance;
					Controller.Instance.lm.accelerationFactor =
						2f; // 1 / speedFactor; -> FEHLER, das klappt nicht... ich hab's daher auf 2 erhöht temporär um mal Filme machen zu können
			
					Controller.Instance.lm.endEffector = pActiveGroup.pEndEffectorPart;	// FEHLER, alles optimieren, nicht jedes mal neu setzen und so Scheisse
					Controller.Instance.lm.Update(true);	// FEHLER, wozu param?
				}

				if((key_resetTarget + key_resetOffset) != 0)
				{
					if(key_resetTarget != 0)
					{
						pActiveGroup.ResetTargetPosition();
						bUserInput = true;
					}

					if(key_resetOffset != 0)
						pActiveGroup.SetTargetOffset(Vector3.zero, Quaternion.identity);
				}
				else if(currentMode == 1)
				{
					// Position wo ich bin -> zeichnet die ServoGroup schon -> ShowLines
		//			DrawRelative(0, pActiveGroup.pEndEffector.transform.position,
		//				pActiveGroup.pEndEffector.transform.up);
		//			DrawRelative(1, pActiveGroup.pEndEffector.transform.position,
		//				pActiveGroup.pEndEffector.transform.right);

					//	w, s -> vor, zurück kippen (höhen) um rechts herum
					//	a, d -> links rechts drehen (seiten) um up herum, in unserem fall forward
					//	q, e -> links rechts drehen (quer) um forward herum in unserem fall up

					Vector3 r = pActiveGroup.targetRotationOffset * pActiveGroup.localEndEffectorRight;
					Vector3 u = pActiveGroup.targetRotationOffset * pActiveGroup.localEndEffectorUp;
					Vector3 f = Vector3.Cross(r, u);

					Vector3 targetPosition; Quaternion targetRotation;
					pActiveGroup.GetTargetPosition(out targetPosition, out targetRotation, false);

				// FEHLER, neu in GetTargetPosition gemacht -> false als letzter Parameter
				//	targetPosition = targetPosition + targetRotation * pActiveGroup.targetPositionOffset;
				//	targetRotation = targetRotation * pActiveGroup.targetRotationOffset;
				
					float factor = bFast ? 0.1f : 0.001f;

					if((key_l & DownType) != 0) // rechts
					{ targetPosition += targetRotation * pActiveGroup.localEndEffectorRight * factor; bUserInput = true; }
					if((key_j & DownType) != 0) // links
					{ targetPosition -= targetRotation * pActiveGroup.localEndEffectorRight * factor; bUserInput = true; }
					if((key_i & DownType) != 0) // rauf / vorwärts
					{ targetPosition += targetRotation * Quaternion.AngleAxis(90f, pActiveGroup.localEndEffectorUp) * pActiveGroup.localEndEffectorRight * factor; bUserInput = true; }
					if((key_k & DownType) != 0) // runter / rückwärts
					{ targetPosition -= targetRotation * Quaternion.AngleAxis(90f, pActiveGroup.localEndEffectorUp) * pActiveGroup.localEndEffectorRight * factor; bUserInput = true; }
					if((key_h & DownType) != 0) // vorwärts / rauf
					{ targetPosition += targetRotation * pActiveGroup.localEndEffectorUp * factor; bUserInput = true; }
					if((key_n & DownType) != 0) // rückwärts / runter
					{ targetPosition -= targetRotation * pActiveGroup.localEndEffectorUp * factor; bUserInput = true; }

					float _s = bFast ? 15f : 1f;

					if((key_w & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(_s, r); bUserInput = true; }
					if((key_s & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(-_s, r); bUserInput = true; }
					if((key_a & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(_s, u); bUserInput = true; }
					if((key_d & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(-_s, u); bUserInput = true; }
					if((key_q & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(_s, f); bUserInput = true; }
					if((key_e & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(-_s, f); bUserInput = true; }

					if(bUserInput)
					{

					if(bDirectMode)
					{
// FEHLER, total anders machen -> jeweils ein Reset nach jedem Frame oder so

						Vector3 vPosition = pActiveGroup.pEndEffectorTransform.position + pActiveGroup.pEndEffectorTransform.rotation * pActiveGroup.targetPositionOffset;

						// dann beschränken

						Vector3 movement = targetPosition - vPosition;

						if(movement.magnitude > maxMovement)
						{
							movement = movement.normalized * maxMovement;
							targetPosition = vPosition + movement;
						}

						Quaternion turning = Quaternion.Inverse(pActiveGroup.pEndEffectorTransform.rotation) * targetRotation;

						float angle; Vector3 axis;
						turning.ToAngleAxis(out angle, out axis);

						if(angle > maxTurning)
						{
							turning = Quaternion.AngleAxis(maxTurning, axis);
							targetRotation = pActiveGroup.pEndEffectorTransform.rotation * turning;
						}
					}

		//			pActiveGroup.SetTargetPosition(
		//				targetPosition - targetRotation * pActiveGroup.targetPositionOffset,
		//				targetRotation * Quaternion.Inverse(pActiveGroup.targetRotationOffset));
			// FEHLER, neu direkt mit SetTargetPosition gemacht -> false als letzten Parameter

					pActiveGroup.SetTargetPosition(targetPosition, targetRotation, false);

					// Position wo ich hin will
			//		DrawRelative(2, pActiveGroup.targetPosition,
			//			pActiveGroup.targetRotation * pActiveGroup.localEndEffectorUp);
			//		DrawRelative(3, pActiveGroup.targetPosition,
			//			pActiveGroup.targetRotation * pActiveGroup.localEndEffectorRight);

					// da muss der doofe erste Servo hin... das ist das Ziel im Moment
		//			DrawRelative(7, pActiveGroup.targetPosition,
		//				pActiveGroup.targetRotation * pActiveGroup.localTargetPositionToServo);

	// FEHLER, supertemp, rotation vom Scheiss prüfen
		//			DrawRelative(9, pActiveGroup.targetPosition + pActiveGroup.targetRotation * pActiveGroup.localTargetPositionToServo,
		//				pActiveGroup.targetRotation * pActiveGroup.localTargetRotationToServo * Vector3.up);
		//			DrawRelative(10, pActiveGroup.targetPosition + pActiveGroup.targetRotation * pActiveGroup.localTargetPositionToServo,
		//				pActiveGroup.targetRotation * pActiveGroup.localTargetRotationToServo * Vector3.right);
					}
				}
				else
				{
					//	w, s -> vor, zurück kippen (höhen) um rechts herum
					//	a, d -> links rechts drehen (seiten) um up herum, in unserem fall forward
					//	q, e -> links rechts drehen (quer) um forward herum in unserem fall up

					Vector3 r = pActiveGroup.targetRotationOffset * pActiveGroup.localEndEffectorRight;
					Vector3 u = pActiveGroup.targetRotationOffset * pActiveGroup.localEndEffectorUp;
					Vector3 f = Vector3.Cross(r, u);

					Vector3 targetPosition = pActiveGroup.pEndEffectorTransform.position + pActiveGroup.pEndEffectorTransform.rotation * pActiveGroup.targetPositionOffset;
					Quaternion targetRotation = pActiveGroup.pEndEffectorTransform.rotation * pActiveGroup.targetRotationOffset;

					bool _bUserInput = false;

					float factor = bFast ? 0.1f : 0.001f;

					if((key_l & DownType) != 0) // rechts
					{ targetPosition += targetRotation * pActiveGroup.localEndEffectorRight * factor; _bUserInput = true; }
					if((key_j & DownType) != 0) // links
					{ targetPosition -= targetRotation * pActiveGroup.localEndEffectorRight * factor; _bUserInput = true; }
					if((key_i & DownType) != 0) // rauf / vorwärts
					{ targetPosition += targetRotation * Quaternion.AngleAxis(90f, pActiveGroup.localEndEffectorUp) * pActiveGroup.localEndEffectorRight * factor; _bUserInput = true; }
					if((key_k & DownType) != 0) // runter / rückwärts
					{ targetPosition -= targetRotation * Quaternion.AngleAxis(90f, pActiveGroup.localEndEffectorUp) * pActiveGroup.localEndEffectorRight * factor; _bUserInput = true; }
					if((key_h & DownType) != 0) // vorwärts / rauf
					{ targetPosition += targetRotation * pActiveGroup.localEndEffectorUp * factor; _bUserInput = true; }
					if((key_n & DownType) != 0) // rückwärts / runter
					{ targetPosition -= targetRotation * pActiveGroup.localEndEffectorUp * factor; _bUserInput = true; }

					float _s = bFast ? 15f : 1f;

					if((key_w & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(_s, r); _bUserInput = true; }
					if((key_s & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(-_s, r); _bUserInput = true; }
					if((key_a & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(_s, u); _bUserInput = true; }
					if((key_d & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(-_s, u); _bUserInput = true; }
					if((key_q & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(_s, f); _bUserInput = true; }
					if((key_e & DownType) != 0)
					{ targetRotation *= Quaternion.AngleAxis(-_s, f); _bUserInput = true; }

					if(_bUserInput)
					{

					pActiveGroup.SetTargetOffset(
						Quaternion.Inverse(pActiveGroup.pEndEffectorTransform.rotation) * (targetPosition - pActiveGroup.pEndEffectorTransform.position),
						Quaternion.Inverse(pActiveGroup.pEndEffectorTransform.rotation) * targetRotation);
					}
				}

				UpdatePointerPosition();
			}

			// calculate IK for the active group
			//			wenn aktiv -> berechnen

			if(bUserInput)
			{
/*
float a = 360f;

float d = 10e-6f;
float dr = a + d;
bool _d = (a == dr);

	-> _d ist true !!! ab hier haben wir ein Problem -> das in IR berücksichtigen und verhindern... echt jetzt
	// wobei es wohl noch andere Probleme geben kann... der mit dem Überschiessen und nicht mehr in den Reset fallen oder sowas...
	der könnte zwar einfach von zu kleinen max acceleration herrühren... das ergäbe nämlich echt Sinn
*/
				SolveIK();
			}

			ResetKeys();
		}

		////////////////////////////////////////
		// Main-Function

		Coroutine SolveIKCoroutine;

		public void SolveIK() // FEHLER, _d später rausnehmen
		{
			if(SolveIKCoroutine != null)
				StopCoroutine(SolveIKCoroutine);

			SolveIKCoroutine = StartCoroutine(_SolveIK());
		}

		public IEnumerator _SolveIK()
		{
			// reset positions to current servo positions
			pActiveGroup.UpdateIK();

			System.Runtime.CompilerServices.StrongBox<bool> res = new System.Runtime.CompilerServices.StrongBox<bool>(false);

			yield return s.SolveIK(res);

			if(res.Value)
			{
	// FEHLER, vom bLimiter des Controller abhängig machen eigentlich... -> sowieso die Scheisse in den Controller rein

				// FEHLER, geht sicher nicht, aber... ich mach's mal zum Spass :-)
	// FEHLER, das move geht eigentlich ganz gut, aber das mit dem Limiter wohl eher nicht... ich mach einfach mal was
				float maxRelRotCommand = 0;

				for(int i = 0; i < pActiveGroup.ikServosForward.Count; i++)
				{
					maxRelRotCommand = Mathf.Max(maxRelRotCommand, Mathf.Abs(pActiveGroup.ikServosForward[i].IKS.TotalRelRotCommand));
				}

				for(int i = 0; i < pActiveGroup.ikServosForward.Count; i++)
				{
					pActiveGroup.ikServosForward[i].IKS.Speed =
						Mathf.Abs(pActiveGroup.ikServosForward[i].IKS.TotalRelRotCommand) * (1f / maxRelRotCommand);
				}


// FEHLER, hier noch auf 360° oder mehr Dreher prüfen

	/*
	pro Servo brauch ich 2 Teildreher... wenn es nicht zwischen den Winkeln ist (alles voll durchgerechnet)
	dann muss ich einen Servo drehen...

	Problem -> 10 Servo = 2 hoch 10 Möglichkeiten... sehr doof das
	*/
	for(int i = 0; i < pActiveGroup.ikServosForward.Count; i++)
	{
		if(pActiveGroup.ikServosForward[i].IKS.IsRotational)
		{
			while(pActiveGroup.ikServosForward[i].IKS.TotalRelRotCommand > 180f)
				pActiveGroup.ikServosForward[i].IKS.TotalRelRotCommand -= 360f;

			while(pActiveGroup.ikServosForward[i].IKS.TotalRelRotCommand < -180f)
				pActiveGroup.ikServosForward[i].IKS.TotalRelRotCommand += 360f;
		}
	}

					bool blocked = false;
					bool retried = false;

					{
					int cnt = pActiveGroup.ikServosForward.Count * 2;

					Vector3 _p;
					Quaternion _q0, _q1;

					pActiveGroup.IKG.Predict(0.0f, out _p, out _q0, int.MaxValue);
					pActiveGroup.IKG.Predict(1.0f, out _p, out _q1, int.MaxValue);

					float maxAngle = Quaternion.Angle(_q0, _q1) + 30f;

	retry_:
					for(int i = 1; i < cnt - 1; i++)
					{
						Quaternion _q;
						pActiveGroup.IKG.Predict((1.0f / cnt) * i, out _p, out _q, int.MaxValue); // FEHLER, statt Execute, mal eine Prediction machen
						// FEHLER, hier prüfen, ob die Info stimmt, wenn ja, nächste Schritte einbauen

						if((Quaternion.Angle(_q0, _q) > maxAngle)
						|| (Quaternion.Angle(_q1, _q) > maxAngle))
						{
							blocked = true;
								// FEHLER, temp, erste Idee... mal sehen ob's ginge
						}
					}

					if(blocked && !retried)
					{
						for(int i = 0; i < pActiveGroup.ikServosForward.Count; i++)
						{
							Quaternion _q;
							pActiveGroup.IKG.Predict(0.5f, out _p, out _q, i + 1);

							if((Quaternion.Angle(_q0, _q) > maxAngle)
							|| (Quaternion.Angle(_q1, _q) > maxAngle))
							{
								if(pActiveGroup.ikServosForward[i].IKS.TotalRelRotCommand > 0f)
									pActiveGroup.ikServosForward[i].IKS.TotalRelRotCommand -= 360f;
								else
									pActiveGroup.ikServosForward[i].IKS.TotalRelRotCommand += 360f;

								blocked = false;
								retried = true;

								goto retry_;
							}
						}
					}

					if(blocked && retried)
						retried = true; // FEHLER, weiss noch nicht was tun... alles experimentell im Moment

					}

					pActiveGroup.Execute(
						(pActiveGroup.servoGroup.GroupSpeedFactor / 2f) * maxDistance,
						1 / speedFactor);
			}
		}

		////////////////////////////////////////
		// IK Interface

		public void Reset()
		{
			SelectActiveGroup(null);

			foreach(ServoGroup g in aAllGroups)
				g.UninitializeIK();

			aAllGroups.Clear();
		}

		public bool SelectActiveGroup(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			if(pActiveGroup != null)
			{
				SetLimiter(pActiveGroup.servoGroup, bLimiter);

				pActiveGroup.Clear();
				pActiveGroup = null;
			}

			if(targetPointer)
				DestroyPointer(ref targetPointer);

			if(positionPointer)
				DestroyPointer(ref positionPointer);

// FEHLER, doof, aber... na ja, halt mal... ok?
			if(g != null)
			{
				ServoGroup sg = QueryGroup(g);

				if(!sg.bIsValidGroup)
				{
					ScreenMessages.PostScreenMessage("invalid group", 5, ScreenMessageStyle.UPPER_CENTER);
					return false;
				}

				pActiveGroup = sg;


				s.SetActiveGroup(pActiveGroup.IKG);


				SetLimiter(pActiveGroup.servoGroup, bLimiter);


				Color green = new Color(0f, 1f, 0f, 0.5f);
				Color red = new Color(1f, 0f, 0f, 0.5f);
				Color blue = new Color(0f, 0f, 1f, 0.5f);

				BuildPointer(out targetPointer, green, red, blue);


			//	Color cyanlike = new Color(0f, 0.78f, 1f, 0.5f);
				Color greend = new Color(0f, 0.7f, 0f, 0.4f);
				Color redd = new Color(0.7f, 0f, 0f, 0.4f);
				Color blued = new Color(0f, 0f, 0.7f, 0.4f);

				BuildPointer(out positionPointer, greend, redd, blued);


				UpdatePointerPosition();

				targetPointer.SetActive(true);
				positionPointer.SetActive(sg.bShowPosition);

				InputLockManager.SetControlLock(ControlTypes.PITCH | ControlTypes.ROLL | ControlTypes.YAW | ControlTypes.THROTTLE | ControlTypes.LINEAR, "_IRIK");

				return true;
			}
			else
			{
				InputLockManager.RemoveControlLock("_IRIK");

				s.SetActiveGroup(null);

				return true;
			}
		}

// FEHLER FEHLER, hier alle umbauen dann, weil neu die Gruppe übermittelt wird für welche das gilt

		public void SetLimiter(InfernalRobotics_v3.Interfaces.IServoGroup g, bool active)
		{
			if((pActiveGroup == null) || (g != pActiveGroup.servoGroup))
				return; // ignore this -> currently not supported

			bLimiter = active;

			if(bLimiter)
			{
				lm.servos = new List<Limiter.ServoLimiter>();

				foreach(ServoWrapper s in pActiveGroup.servos)
				{
					Limiter.ServoLimiter sl = new Limiter.ServoLimiter();
					sl.servo = s.servo.servo;
					((InfernalRobotics_v3.Module.ModuleIRServo_v3)s.servo.servo).RegisterLimiter(sl);
					lm.servos.Add(sl);
				}

				lm.endEffector = pActiveGroup.pEndEffectorPart;
			}
			else
			{
				foreach(ServoWrapper s in pActiveGroup.servos)
					((InfernalRobotics_v3.Module.ModuleIRServo_v3)s.servo.servo).RegisterLimiter(null);

				lm.servos = null;
			}
		}

		public bool GetLimiter(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			return bLimiter;
		}

		public void SetDirectMode(InfernalRobotics_v3.Interfaces.IServoGroup g, bool active)
		{
			bDirectMode = active;
	//		if(ob != null)
	//			ob.SetActive(!bDirectMode); -> FEHLER, das evtl. per anderem Setting lösen?
		}

		public bool GetDirectMode(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			return bDirectMode;
		}

		public void Relax(InfernalRobotics_v3.Interfaces.IServoGroup g, int factor)
		{
			if((pActiveGroup == null) || (g != pActiveGroup.servoGroup))
				return; // ignore this -> currently not supported

			StartCoroutine(RelaxGroup(pActiveGroup, factor));
		}

		private IEnumerator RelaxGroup(ServoGroup g, int factor)
		{
// FEHLER, welcher Mode soll man wählen? sowieso, was soll man tun hier?

			foreach(InfernalRobotics_v3.Interfaces.IServo s in g.Servos)
				s.SetRelaxMode(1f);

			int i = factor;

			while(i-- > 0)
			{
				foreach(InfernalRobotics_v3.Interfaces.IServo s in g.Servos)
					s.RelaxStep();

				yield return new WaitForFixedUpdate();
			}

			foreach(InfernalRobotics_v3.Interfaces.IServo s in g.Servos)
				s.ResetRelaxMode();
		}

		public void SelectEndEffector(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			if(pActiveGroup.StartSelectEndEffector())
			{
				if(endEffectorCoroutine != null)
				{
					StopCoroutine(endEffectorCoroutine);
					endEffectorCoroutine = null;
				}

				if(endEffectorCoroutinePart)
					endEffectorCoroutinePart.SetHighlightDefault();

				if(pActiveGroup.pEndEffectorPart)
				{
					pActiveGroup.pEndEffectorPart.SetHighlight(true, false);
					pActiveGroup.pEndEffectorPart.SetHighlightColor(new Color(1.0f, 0.5f, 0.0f));
					pActiveGroup.pEndEffectorPart.SetHighlightType(Part.HighlightType.AlwaysOn);
				}
			}
		}

		public bool OnEndEffectorSelected(Part p)
		{
			ServoGroup sg = ServoGroup.pSetEndEffectorGroup;

			bool bRes = ServoGroup.pSetEndEffectorGroup.EndSetEndEffector(p);
				// FEHLER, das mit dem pSetEndEffectorGroup -> ist das gut dort oder soll das in den Controller? wer macht das "Update"-Handling?

			if(bRes)
			{
				sg.UpdateIK();		// if an existing group is used, we must update before re-initializing
				sg.InitializeIK();

				sg.ResetTargetPosition();


// FEHLER, ist das ok so?
				s.SetActiveGroup(pActiveGroup.IKG);
			}

			return bRes;
		}

		public bool GetShowPosition(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			ServoGroup sg = GetGroup(g);
			return (sg != null) ? sg.bShowPosition : false;
		}

		public void SetShowPosition(InfernalRobotics_v3.Interfaces.IServoGroup g, bool show)
		{
			ServoGroup sg = GetGroup(g);
			if(sg == null) return;

			sg.bShowPosition = show;

			if(positionPointer)
				positionPointer.SetActive(show);
		}

		bool bSelectTarget = false;

		public void SelectTarget(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			bSelectTarget = !bSelectTarget;
		}

		public void SetTarget(InfernalRobotics_v3.Interfaces.IServoGroup g, Vector3 targetPosition, Quaternion targetRotation)
		{
			ServoGroup sg = GetGroup(g);
			if(sg != null)
				sg.SetTargetPosition(targetPosition, targetRotation, true);
		}

		public void GetTarget(InfernalRobotics_v3.Interfaces.IServoGroup g, out Vector3 position, out Quaternion rotation)
		{
			ServoGroup sg = GetGroup(g);
			if(sg == null)
			{
				position = Vector3.zero;
				rotation = Quaternion.identity;
			}
			else
				sg.GetTargetPosition(out position, out rotation, true);
		}

		public void Action1(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			s.Action1(g);
		}

		public void Action2(InfernalRobotics_v3.Interfaces.IServoGroup g)
		{
			s.Action2(g);
		}
	}
}
