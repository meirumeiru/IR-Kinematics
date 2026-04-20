using System.Collections.Generic;
using System.IO;

using UnityEngine;


namespace IR_Kinematics.Solver
{
	public interface IIKPartWrapper
	{
		uint PersistentId { get; }

		void SetPosition(Vector3 p_position, Quaternion p_rotation, float pos);
	}
}
