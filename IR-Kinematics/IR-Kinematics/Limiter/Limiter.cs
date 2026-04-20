using System.Collections.Generic;

using UnityEngine;


namespace IR_Kinematics.Limiter
{
	// class to limit manually given commands

	public class Limiter
	{
		public Part endEffector;				// FEHLER, doof, weil wir ja beim anderen 'n Transform haben und ein Part -> hier später auch aufteilen, ist aber aktuell etwas egal
		public List<ServoLimiter> servos;

		public float maxDistance;
		public float accelerationFactor;

		void CalculateLimit(ServoLimiter s)
		{
			s.endEffectorDistance = endEffector.transform.position - ((InfernalRobotics_v3.Module.ModuleIRServo_v3)s.servo.servo).transform.position;

			s.endEffectorDistance =
				Vector3.ProjectOnPlane(s.endEffectorDistance, ((InfernalRobotics_v3.Module.ModuleIRServo_v3)s.servo.servo).GetAxis());

			// largestDistance -> FEHLER, später vielleicht mal... nicht jetzt
		}

		void CalculateLimits()
		{
			for(int i = 0; i < servos.Count; i++)
				CalculateLimit(servos[i]);
		}

		public void Update(bool bActive)
		{
			CalculateLimits();

			for(int i = 0; i < servos.Count; i++)
			{
				servos[i].bActive = bActive;

				servos[i].maxSpeed = maxDistance / servos[i].endEffectorDistance.magnitude;
					// FEHLER, der Faktor, das ist doch Müll so, den muss man doch noch korrigieren oder so

				servos[i].accelerationFactor = accelerationFactor;
			}
		}
	}
}
