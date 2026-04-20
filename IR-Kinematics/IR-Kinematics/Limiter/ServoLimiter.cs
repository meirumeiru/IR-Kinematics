using System;

using UnityEngine;


namespace IR_Kinematics.Limiter
{
	public class ServoLimiter : InfernalRobotics_v3.Interfaces.ILimiter
	{
		public InfernalRobotics_v3.Interfaces.IServo servo;
		public float accelerationFactor;

		public bool bActive = false;

		public Vector3 endEffectorDistance; // FEHLER, temp
		public float maxSpeed;

		public bool SetCommand(ref float p_TargetPosition, ref float p_TargetSpeed, ref float p_Acceleration)
		{
			if(bActive)
			{
				float speed = Math.Min(p_TargetSpeed, maxSpeed);

				p_TargetSpeed = speed;
				p_Acceleration = speed * accelerationFactor;
			}

			return bActive;
		}
	}
}
