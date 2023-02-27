using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*	From video Coding adventure Boids: https://www.youtube.com/watch?v=bqtqltqcQhw&t=302s
 * 	
 *	Sample pseudo uniform points on a sphere using golden ratio
*/

public static class CreateRayDirectionsSphere {

	public const int numPoints = 300;
	public static readonly Vector3[] directions;

	static CreateRayDirectionsSphere () {
		directions = new Vector3[CreateRayDirectionsSphere.numPoints];

		float goldenRatio = (1 + Mathf.Sqrt (5)) / 2;
		float angleIncrement = Mathf.PI * 2 * goldenRatio; //produces good, almost uniformly spaced points

		for (int i = 0; i < numPoints; i++) {
			float t = (float) i / numPoints;
			//spherical coordinates
			float polarAngle = Mathf.Acos (1 - 2 * t);
			float azimuth = angleIncrement * i;

			float x = Mathf.Sin (polarAngle) * Mathf.Cos (azimuth);
			float y = Mathf.Sin (polarAngle) * Mathf.Sin (azimuth);
			float z = Mathf.Cos (polarAngle);
			directions[i] = new Vector3 (x, y, z);
		}
	}

}