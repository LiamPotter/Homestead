/*-----------------------------+------------------------------\
|                                                             |
|                        !!!NOTICE!!!                         |
|                                                             |
|  These libraries are under heavy development so they are    |
|  subject to make many changes as development continues.     |
|  For this reason, the libraries may not be well commented.  |
|  THANK YOU for supporting forge with all your feedback      |
|  suggestions, bug reports and comments!                     |
|                                                             |
|                               - The Forge Team              |
|                                 Bearded Man Studios, Inc.   |
|                                                             |
|  This source code, project files, and associated files are  |
|  copyrighted by Bearded Man Studios, Inc. (2012-2015) and   |
|  may not be redistributed without written permission.       |
|                                                             |
\------------------------------+-----------------------------*/


using UnityEditor;

using System.Collections.Generic;

using BeardedManStudios.Network;

[CustomEditor(typeof(SimpleNetworkedMonoBehavior), true), CanEditMultipleObjects]
public class SNMB_Parent_Editor : Editor
{
	//[MenuItem("Window/Forge Networking/Compile Scene")]
	private void CompileScene()
	{
		int id = 0;

		List<SimpleNetworkedMonoBehavior> behaviors = new List<SimpleNetworkedMonoBehavior>();
		NetworkingManager manager = FindObjectOfType<NetworkingManager>();

		if (manager != null)
		{
			behaviors.Add(manager);

            if (manager.startNetworkedSceneBehaviors != null)
            {
                foreach (SimpleNetworkedMonoBehavior behavior in manager.startNetworkedSceneBehaviors)
                {
                    if (behavior == null)
                        continue;

                    if (!behaviors.Contains(behavior))
                        behaviors.Add(behavior);
                }
            }
            else
                manager.startNetworkedSceneBehaviors = new SimpleNetworkedMonoBehavior[0];
		}
		else
			id++;

		foreach (SimpleNetworkedMonoBehavior behavior in FindObjectsOfType<SimpleNetworkedMonoBehavior>())
		{
			if (!behaviors.Contains(behavior))
				behaviors.Add(behavior);
		}

		foreach (SimpleNetworkedMonoBehavior behavior in behaviors)
			behavior.SetSceneNetworkedId(id++);
	}

	protected virtual void OnEnable()
	{
		CompileScene();
	}

	private void OnDisable()
	{
		CompileScene();
	}
}