using System.Collections.Generic;
using System.IO;

using UnityEngine;


namespace IR_Kinematics.Solver
{
	public interface IIKServoWrapper : IIKPartWrapper
	{
		void SetPointerPart(uint p_pointerPartPersistentId, Vector3 p_localPointerPartPosition);

		float TotalRelRotCommand { get; set; }
		float Speed { get; set; }

		Vector3 Position { get; }

		bool IsRotational { get; }

		Vector3 GetAxis();
		Vector3 GetSecAxis();

		Vector3 GetAxisGlobal();
		Vector3 GetSecAxisGlobal();
	}
}
