using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*	
 *	Sample uniform directions to the edge of a disk,
 *	starting in front and following to the back of unit circle
*/

public static class CreateRayDirectionsDisk {

	public const int numPoints = 50;
	public static readonly Vector3[] directions;

	static CreateRayDirectionsDisk () {
		directions = new Vector3[CreateRayDirectionsDisk.numPoints];
		float[] angles = new float[CreateRayDirectionsDisk.numPoints];

		//partition circle on equal angles
		for (int i = 0; i < numPoints; i++) {
			float ratio = (float) i / numPoints;
			//polar coordinates
			angles[i] =  2*Mathf.PI * ratio;
		}

		//sort angles according to their Sin
		Array.Sort(angles, (a, b) => {
			// Do compare code here. a and b are two objects being compared to
			if(Mathf.Sin(a) > Mathf.Sin(b)) {
				return -1;
			} else if(Mathf.Sin(a) < Mathf.Sin(b)) {
				return 1;
			}

			return 0;
		} );
			
		//calculate vectors in that order
		for (int i = 0; i < numPoints; i++) {
			float theta = angles[i];
				
			float x = Mathf.Cos(theta);
			float y = 0;
			float z = Mathf.Sin(theta);
			directions[i] = new Vector3 (x, y, z);
		}
	}

}