using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;


namespace IR_Kinematics.Solver
{
	public interface IIKServoGroup
	{
		void GetTargetPosition(out Vector3 currentTargetPosition, out Quaternion currentTargetRotation);
		void SetTargetPosition(Vector3 newTargetPosition, Quaternion newTargetRotation);

		void Predict(float factor, out Vector3 position, out Quaternion rotation, int count);
	}
}
