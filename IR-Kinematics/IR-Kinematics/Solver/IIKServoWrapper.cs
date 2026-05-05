using System.Collections.Generic;
using System.IO;

using UnityEngine;


namespace IR_Kinematics.Solver
{
	public interface IIKServoWrapper : IIKPartWrapper
	{
		void UpdatePosition(Vector3 p_position, Quaternion p_rotation, float pos);

		void SetPointerPart(IIKPartWrapper p_pointerPart);

		float TotalRelRotCommand { get; set; }
		float Speed { get; set; }

		bool IsRotational { get; }

		Vector3 GetAxis();
		Vector3 GetSecAxis();

		Vector3 GetAxisGlobal();
		Vector3 GetSecAxisGlobal();
	}
}
