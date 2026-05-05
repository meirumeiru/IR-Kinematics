using System.Collections.Generic;
using System.IO;

using UnityEngine;


namespace IR_Kinematics.Solver
{
	public interface IIKPartWrapper
	{
		uint PersistentId { get; }

		void UpdatePosition(Vector3 p_position, Quaternion p_rotation);

		Vector3 Position { get; }
		Quaternion Rotation { get; }
	}
}
