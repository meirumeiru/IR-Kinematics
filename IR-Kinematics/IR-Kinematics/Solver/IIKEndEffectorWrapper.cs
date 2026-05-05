using System.Collections.Generic;
using System.IO;

using UnityEngine;


namespace IR_Kinematics.Solver
{
	public interface IIKEndEffectorWrapper : IIKPartWrapper
	{
		Vector3 Up { get; }
		Vector3 Right { get; }
	}
}
