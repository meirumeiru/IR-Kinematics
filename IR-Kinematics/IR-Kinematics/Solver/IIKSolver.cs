using System.Collections.Generic;


using UnityEngine;


namespace IR_Kinematics.Solver
{
	public interface IIKSolver
	{
		// Parts, Servos

		IIKPartWrapper Create_IKPartWrapper(uint p_persistentId, Vector3 p_position, Quaternion p_rotation);

		IIKServoWrapper Create_IKServoWrapper(uint p_persistentId, Vector3 p_position, Quaternion p_rotation, Vector3 p_axis, Vector3 p_secAxis, Vector3 p_anchor, bool p_rotational, bool p_limited);

		// Groups

		void InitializeIK(bool isReversed,
			List<Command.ServoWrapper> ikServosForward, List<Command.ServoWrapper> ikServosBackward,
			Transform pEndEffectorTransform, Part rootPart,
			out Solver.IIKServoGroup ikg, out bool bIsValidGroup);

		void UninitializeIK(Solver.IIKServoGroup ikg);

		// Solver

		void Create_IKSolver();
		
		void SetActiveGroup(Solver.IIKServoGroup ikg);

		System.Collections.IEnumerator SolveIK(System.Runtime.CompilerServices.StrongBox<bool> res);

		void Action1(InfernalRobotics_v3.Interfaces.IServoGroup g);
		void Action2(InfernalRobotics_v3.Interfaces.IServoGroup g);
	}
}
