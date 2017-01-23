using UnityEngine;
using System.Collections;
using Rewired;
using TriTools;

namespace TriTools
{
    public class TriToolHub : MonoBehaviour
    {

        #region Enums
        public enum XYZ
        {
            X,
            Y,
            Z
        }

        public enum AxisPlane
        {
            XZ,
            XY,
            YZ
        }

        public enum Operation
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Min,
            Max
        }
        public enum InterpolationType
        {
            Linear = 0,
            EaseInOut = 1
        }
        public enum AffectType
        {
            Rotation,
            Position
        }
        #endregion

        #region Smooth Look At Direction
        public static void SmoothLookAtDirection(GameObject toRotate, Vector3 targetDirection, float minMagnitude, Vector3 upVector, bool keepUpright, float speed)
        {
            GameObject previousGo = null;
            Quaternion lastRotation = Quaternion.identity;
            Quaternion desiredRotation = Quaternion.identity;


            GameObject go = toRotate;

            // re-initialize if game object has changed

            if (previousGo != go)
            {
                lastRotation = go.transform.rotation;
                desiredRotation = lastRotation;
                previousGo = go;
            }

            // desired direction

            Vector3 diff = targetDirection;

            if (keepUpright)
            {
                diff.y = 0;
            }

            if (diff.sqrMagnitude > minMagnitude)
            {
                desiredRotation = Quaternion.LookRotation(diff, upVector);
            }

            lastRotation = Quaternion.Slerp(lastRotation, desiredRotation, speed * Time.deltaTime);
            go.transform.rotation = lastRotation;

        }
        public static void SmoothLookAtDirection(Transform toRotate, Vector3 targetDirection, float minMagnitude, Vector3 upVector, bool keepUpright, float speed)
        {
            GameObject previousGo = null;
            Quaternion lastRotation = Quaternion.identity;
            Quaternion desiredRotation = Quaternion.identity;


            GameObject go = toRotate.gameObject;

            // re-initialize if game object has changed

            if (previousGo != go)
            {
                lastRotation = go.transform.rotation;
                desiredRotation = lastRotation;
                previousGo = go;
            }

            // desired direction

            Vector3 diff = targetDirection;

            if (keepUpright)
            {
                diff.y = 0;
            }

            if (diff.sqrMagnitude > minMagnitude)
            {
                desiredRotation = Quaternion.LookRotation(diff, upVector);
            }

            lastRotation = Quaternion.Slerp(lastRotation, desiredRotation, speed * Time.deltaTime);
            go.transform.rotation = lastRotation;

        }
        #endregion

        #region Smooth Look At
        public static void SmoothLookAt(GameObject gameObject, GameObject target, Vector3 upDirection, bool keepUpright, float speed)
        {

            Quaternion lastRotation;
            Quaternion desiredRotation;
            var go = gameObject;

            var goTarget = target;

            lastRotation = go.transform.rotation;
            desiredRotation = lastRotation;

            Vector3 lookAtPos;
            lookAtPos = goTarget.transform.position;
            if (keepUpright)
                lookAtPos.y = go.transform.position.y;

            var diff = lookAtPos - go.transform.position;
            if (diff != Vector3.zero && diff.sqrMagnitude > 0)
            {
                desiredRotation = Quaternion.LookRotation(diff, upDirection);
            }

            lastRotation = Quaternion.Slerp(lastRotation, desiredRotation, speed * Time.deltaTime);
            go.transform.rotation = lastRotation;


        }

        public static void SmoothLookAt(GameObject gameObject, GameObject target, bool keepUpright, float speed)
        {

            Quaternion lastRotation;
            Quaternion desiredRotation;
            var go = gameObject;

            var goTarget = target;

            lastRotation = go.transform.rotation;
            desiredRotation = lastRotation;

            Vector3 lookAtPos;
            lookAtPos = goTarget.transform.position;
            if (keepUpright)
                lookAtPos.y = go.transform.position.y;

            var diff = lookAtPos - go.transform.position;
            if (diff != Vector3.zero && diff.sqrMagnitude > 0)
            {
                desiredRotation = Quaternion.LookRotation(diff, Vector3.up);
            }

            lastRotation = Quaternion.Slerp(lastRotation, desiredRotation, speed * Time.deltaTime);
            go.transform.rotation = lastRotation;


        }

        public static void SmoothLookAt(GameObject gameObject, GameObject target, float speed)
        {

            Quaternion lastRotation;
            Quaternion desiredRotation;
            var go = gameObject;

            var goTarget = target;

            lastRotation = go.transform.rotation;
            desiredRotation = lastRotation;

            Vector3 lookAtPos;
            lookAtPos = goTarget.transform.position;
            var diff = lookAtPos - go.transform.position;
            if (diff != Vector3.zero && diff.sqrMagnitude > 0)
            {
                desiredRotation = Quaternion.LookRotation(diff, Vector3.up);
            }

            lastRotation = Quaternion.Slerp(lastRotation, desiredRotation, speed * Time.deltaTime);
            go.transform.rotation = lastRotation;


        }

        public static void SmoothLookAt(GameObject gameObject, Vector3 target, Vector3 upDirection, bool keepUpright, float speed)
        {

            Quaternion lastRotation;
            Quaternion desiredRotation;
            var go = gameObject;

            var goTarget = target;

            lastRotation = go.transform.rotation;
            desiredRotation = lastRotation;

            Vector3 lookAtPos;
            lookAtPos = target;
            if (keepUpright)
                lookAtPos.y = go.transform.position.y;

            var diff = lookAtPos - go.transform.position;
            if (diff != Vector3.zero && diff.sqrMagnitude > 0)
            {
                desiredRotation = Quaternion.LookRotation(diff, upDirection);
            }

            lastRotation = Quaternion.Slerp(lastRotation, desiredRotation, speed * Time.deltaTime);
            go.transform.rotation = lastRotation;


        }

        public static void SmoothLookAt(GameObject gameObject, Vector3 target, bool keepUpright, float speed)
        {

            Quaternion lastRotation;
            Quaternion desiredRotation;
            var go = gameObject;

            var goTarget = target;

            lastRotation = go.transform.rotation;
            desiredRotation = lastRotation;

            Vector3 lookAtPos;
            lookAtPos = target;
            if (keepUpright)
                lookAtPos.y = go.transform.position.y;

            var diff = lookAtPos - go.transform.position;
            if (diff != Vector3.zero && diff.sqrMagnitude > 0)
            {
                desiredRotation = Quaternion.LookRotation(diff, Vector3.up);
            }

            lastRotation = Quaternion.Slerp(lastRotation, desiredRotation, speed * Time.deltaTime);
            go.transform.rotation = lastRotation;


        }

        public static void SmoothLookAt(GameObject gameObject, Vector3 target, float speed)
        {

            Quaternion lastRotation;
            Quaternion desiredRotation;
            var go = gameObject;

            var goTarget = target;

            lastRotation = go.transform.rotation;
            desiredRotation = lastRotation;

            Vector3 lookAtPos;
            lookAtPos = target;
            var diff = lookAtPos - go.transform.position;
            if (diff != Vector3.zero && diff.sqrMagnitude > 0)
            {
                desiredRotation = Quaternion.LookRotation(diff, Vector3.up);
            }

            lastRotation = Quaternion.Slerp(lastRotation, desiredRotation, speed * Time.deltaTime);
            go.transform.rotation = lastRotation;


        }
        #endregion

        public static void SetAnimatorLayerWeight(GameObject targetAnimator, int layerIndex, float layerWeight)
        {
            Animator _animator;
            // get the animator component
            GameObject go = targetAnimator;

            _animator = go.GetComponent<Animator>();


            _animator.SetLayerWeight(layerIndex, layerWeight);

        }

        public static void SetAnimatorBool(GameObject targetAnimator, string parameter, bool value)
        {
            GameObject go = targetAnimator;
            Animator _animator;
            int _paramID;
            _animator = go.GetComponent<Animator>();

            // get hash from the param for efficiency:
            _paramID = Animator.StringToHash(parameter);
            _animator.SetBool(_paramID, value);

        }

        #region Set Animator Float
        public static void SetAnimatorFloat(GameObject targetAnimator, string animationParameter, float value, float dampTime)
        {
            Animator _animator;
            int _paramID;

            GameObject go = targetAnimator;
            _animator = go.GetComponent<Animator>();
            _paramID = Animator.StringToHash(animationParameter);
            _animator.SetFloat(_paramID, value, dampTime, Time.deltaTime);
        }
        public static void SetAnimatorFloat(GameObject targetAnimator, string animationParameter, float value)
        {
            Animator _animator;
            int _paramID;

            GameObject go = targetAnimator;
            _animator = go.GetComponent<Animator>();
            _paramID = Animator.StringToHash(animationParameter);
            _animator.SetFloat(_paramID, value);
        }
        #endregion

        public static void SetAnimatorInt(GameObject targetAnimator, string animationParameter, int value)
        {
            Animator _animator;
            int _paramID;

            GameObject go = targetAnimator;
            _animator = go.GetComponent<Animator>();
            _paramID = Animator.StringToHash(animationParameter);
            _animator.SetInteger(_paramID, value);
        }

        public static void SetPosition(GameObject target, Vector3 moveVector, Space space)
        {
            GameObject pTarget = target;

            //Vector3 position = space==Space.World?pTarget.transform.position:pTarget.transform.localPosition;
            Vector3 position = moveVector;


            if (space == Space.World)
                pTarget.transform.position = position;
            else
                pTarget.transform.localPosition = position;
        }

        public static void GetAxis(string axisName, float multiplier, out float toStoreFloat)
        {
            float axisValue = Input.GetAxis(axisName);
            axisValue *= multiplier;
            toStoreFloat = axisValue;
        }

        public static void GetAxis(int playerNum, string axisName, float multiplier, out float toStoreFloat)
        {
            float axisValue = ReInput.players.GetPlayer(playerNum).GetAxis(axisName);
            axisValue *= multiplier;
            toStoreFloat = axisValue;
        }

        #region Get Axis Vector
        public static void GetAxisVector(string horizontalAxis, string verticalAxis, float multiplier, AxisPlane mapToPlane, GameObject relativeTo, out Vector3 toStoreVector)
        {
            var forward = new Vector3();
            var right = new Vector3();

            Transform transform = relativeTo.transform;

            switch (mapToPlane)
            {
                case AxisPlane.XZ:
                    forward = transform.TransformDirection(Vector3.forward);
                    forward.y = 0;
                    forward = forward.normalized;
                    right = new Vector3(forward.z, 0, -forward.x);
                    break;

                case AxisPlane.XY:
                case AxisPlane.YZ:
                    // NOTE: in relative mode XY ans YZ are the same!
                    forward = Vector3.up;
                    forward.z = 0;
                    forward = forward.normalized;
                    right = transform.TransformDirection(Vector3.right);
                    break;
            }
            // get individual axis

            float h = Input.GetAxis(horizontalAxis);
            float v = Input.GetAxis(verticalAxis);

            // calculate resulting direction vector

            Vector3 direction = h * right + v * forward;
            direction *= multiplier;
            toStoreVector = direction;


        }
        public static void GetAxisVector(int playerNum, string horizontalAxis, string verticalAxis, float multiplier, AxisPlane mapToPlane, GameObject relativeTo, out Vector3 toStoreVector)
        {
            var forward = new Vector3();
            var right = new Vector3();

            Transform transform = relativeTo.transform;

            switch (mapToPlane)
            {
                case AxisPlane.XZ:
                    forward = transform.TransformDirection(Vector3.forward);
                    forward.y = 0;
                    forward = forward.normalized;
                    right = new Vector3(forward.z, 0, -forward.x);
                    break;

                case AxisPlane.XY:
                case AxisPlane.YZ:
                    // NOTE: in relative mode XY ans YZ are the same!
                    forward = Vector3.up;
                    forward.z = 0;
                    forward = forward.normalized;
                    right = transform.TransformDirection(Vector3.right);
                    break;
            }
            // get individual axis

            float h = ReInput.players.GetPlayer(playerNum).GetAxis(horizontalAxis);
            float v = ReInput.players.GetPlayer(playerNum).GetAxis(verticalAxis);

            // calculate resulting direction vector

            Vector3 direction = h * right + v * forward;
            direction *= multiplier;
            toStoreVector = direction;


        }
        public static void GetAxisVector(string horizontalAxis, string verticalAxis, float multiplier, AxisPlane mapToPlane, out Vector3 toStoreVector)
        {
            var forward = new Vector3();
            var right = new Vector3();

            switch (mapToPlane)
            {
                case AxisPlane.XZ:
                    forward = Vector3.forward;
                    right = Vector3.right;
                    break;

                case AxisPlane.XY:
                    forward = Vector3.up;
                    right = Vector3.right;
                    break;

                case AxisPlane.YZ:
                    forward = Vector3.up;
                    right = Vector3.forward;
                    break;
            }
            // get individual axis

            float h = Input.GetAxis(horizontalAxis);
            float v = Input.GetAxis(verticalAxis);

            // calculate resulting direction vector

            Vector3 direction = h * right + v * forward;
            direction *= multiplier;
            toStoreVector = direction;
        }
        public static void GetAxisVector(int playerNum, string horizontalAxis, string verticalAxis, float multiplier, AxisPlane mapToPlane, out Vector3 toStoreVector)
        {
            var forward = new Vector3();
            var right = new Vector3();


            switch (mapToPlane)
            {
                case AxisPlane.XZ:
                    forward = Vector3.forward;
                    right = Vector3.right;
                    break;

                case AxisPlane.XY:
                    forward = Vector3.up;
                    right = Vector3.right;
                    break;

                case AxisPlane.YZ:
                    forward = Vector3.up;
                    right = Vector3.forward;
                    break;
            }
            // get individual axis

            float h = ReInput.players.GetPlayer(playerNum).GetAxis(horizontalAxis);
            float v = ReInput.players.GetPlayer(playerNum).GetAxis(verticalAxis);

            // calculate resulting direction vector

            Vector3 direction = h * right + v * forward;
            direction *= multiplier;
            toStoreVector = direction;


        }
        #endregion

        public static void CreateVector3(float horizontal, float vertical, float multiplier, AxisPlane mapToPlane, GameObject relativeTo, out Vector3 toStoreVector)
        {
            var forward = new Vector3();
            var right = new Vector3();
            Transform transform = relativeTo.transform;

            switch (mapToPlane)
            {
                case AxisPlane.XZ:
                    forward = transform.TransformDirection(Vector3.forward);
                    forward.y = 0;
                    forward = forward.normalized;
                    right = new Vector3(forward.z, 0, -forward.x);
                    break;

                case AxisPlane.XY:
                case AxisPlane.YZ:
                    // NOTE: in relative mode XY ans YZ are the same!
                    forward = Vector3.up;
                    forward.z = 0;
                    forward = forward.normalized;
                    right = transform.TransformDirection(Vector3.right);
                    break;
            }
            float h = horizontal;
            float v = vertical;

            Vector3 direction = h * right + v * forward;
            direction *= multiplier;
            toStoreVector = direction;
        }

        #region GetVector3
        public static void GetVector3(Vector3 vectorVariable, out float storeX, out float storeY, out float storeZ)
        {

            //if (storeX != null)
            storeX = vectorVariable.x;

            //if (storeY != null)
            storeY = vectorVariable.y;

            //if (storeZ != null)
            storeZ = vectorVariable.z;
        }
        public static void GetVector3(Vector3 vectorVariable, XYZ axisToGet, out float storeFloat)
        {
            switch (axisToGet)
            {
                case XYZ.X:
                    storeFloat = vectorVariable.x;
                    break;
                case XYZ.Y:
                    storeFloat = vectorVariable.y;
                    break;
                case XYZ.Z:
                    storeFloat = vectorVariable.z;
                    break;
                default:
                    storeFloat = 0f;
                    Debug.LogError("No Axis Provided!");
                    break;
            }
        }
        #endregion

        #region Set Velocity
        public static void SetVelocity(GameObject toAffect, Vector3 vector, Space space)
        {
            GameObject go = toAffect;

            Vector3 velocity;

            Rigidbody rBody = toAffect.GetComponent<Rigidbody>();

            velocity = vector;

            if (space == Space.World)
            {
                rBody.velocity = velocity;
            }
            else
            {
                rBody.velocity = go.transform.TransformDirection(velocity);
            }
        }
        public static void SetVelocity(GameObject toAffect, Vector3 vector, float speed, Space space)
        {
            GameObject go = toAffect;

            Vector3 velocity;

            velocity = vector * speed;

            if (space == Space.World)
            {
                toAffect.GetComponent<Rigidbody>().velocity = velocity;
            }
            else
            {
                toAffect.GetComponent<Rigidbody>().velocity = go.transform.TransformDirection(velocity);
            }
        }
        public static void SetVelocity(GameObject toAffect, XYZ axis, float amount, Space space)
        {
            GameObject go = toAffect;

            Vector3 velocity = toAffect.GetComponent<Rigidbody>().velocity;

            Rigidbody rBody = toAffect.GetComponent<Rigidbody>();
            switch (axis)
            {
                case XYZ.X:
                    velocity.x = amount;
                    break;
                case XYZ.Y:
                    velocity.y = amount;
                    break;
                case XYZ.Z:
                    velocity.z = amount;
                    break;
                default:
                    break;
            }

            if (space == Space.World)
            {
                rBody.velocity = velocity;
            }
            else
            {
                rBody.velocity = go.transform.TransformDirection(velocity);
            }
        }

        public static void SetVelocity(GameObject toAffect, XYZ axis1, float amount1, XYZ axis2, float amount2, Space space)
        {
            SetVelocity(toAffect, axis1, amount1, space);
            SetVelocity(toAffect, axis2, amount2, space);
        }
        public static void SetVelocity(GameObject toAffect, Vector3 vectorVariable, XYZ axis, float amount, Space space)
        {
            GameObject go = toAffect;

            Vector3 velocity = vectorVariable;

            Rigidbody rBody = toAffect.GetComponent<Rigidbody>();

            switch (axis)
            {
                case XYZ.X:
                    velocity.x = amount;
                    break;
                case XYZ.Y:
                    velocity.y = amount;
                    break;
                case XYZ.Z:
                    velocity.z = amount;
                    break;
                default:
                    break;
            }
            if (space == Space.World)
            {
                rBody.velocity = velocity;
            }
            else
            {
                rBody.velocity = go.transform.TransformDirection(velocity);
            }
            //toAffect.GetComponent<Rigidbody>().velocity = space == Space.World ? velocity : go.transform.TransformDirection(velocity);
        }
        public static void SetVelocity(GameObject toAffect, Vector3 vectorVariable, float x, float y, float z, Space space)
        {
            GameObject go = toAffect;

            Vector3 velocity = vectorVariable;

            Rigidbody rBody = toAffect.GetComponent<Rigidbody>();

            velocity.x = x;

            velocity.y = y;

            velocity.z = z;


            if (space == Space.World)
            {
                rBody.velocity = velocity;
            }
            else
            {
                rBody.velocity = go.transform.TransformDirection(velocity);
            }
        }
        #endregion

        #region Add Force
        public static void AddForce(GameObject gameObjectToAffect, Vector3 atPosition, Vector3 forceToAdd, float x, float y, float z, Space space, ForceMode forceMode)
        {
            GameObject go = gameObjectToAffect;

            Vector3 force = forceToAdd;

            force.x = x;
            force.y = y;
            force.z = z;

            if (space == Space.World)
            {
                go.GetComponent<Rigidbody>().AddForceAtPosition(force, atPosition, forceMode);
            }
            else
            {
                go.GetComponent<Rigidbody>().AddRelativeForce(force, forceMode);
            }
        }
        public static void AddForce(GameObject gameObjectToAffect, Vector3 forceToAdd, float x, float y, float z, Space space, ForceMode forceMode)
        {
            GameObject go = gameObjectToAffect;

            Vector3 force = forceToAdd;

            force.x = x;
            force.y = y;
            force.z = z;

            if (space == Space.World)
            {
                {
                    go.GetComponent<Rigidbody>().AddForce(force, forceMode);
                }
            }
            else
            {
                go.GetComponent<Rigidbody>().AddRelativeForce(force, forceMode);
            }
        }
        public static void AddForce(GameObject gameObjectToAffect, XYZ axisToAdd, float amount, Space space, ForceMode forceMode)
        {
            GameObject go = gameObjectToAffect;

            Vector3 force = new Vector3();
            switch (axisToAdd)
            {
                case XYZ.X:
                    force.x = amount;
                    break;
                case XYZ.Y:
                    force.y = amount;
                    break;
                case XYZ.Z:
                    force.z = amount;
                    break;
                default:
                    break;
            }


            if (space == Space.World)
            {
                {
                    go.GetComponent<Rigidbody>().AddForce(force, forceMode);
                }
            }
            else
            {
                go.GetComponent<Rigidbody>().AddRelativeForce(force, forceMode);
            }
        }
        public static void AddForce(GameObject gameObjectToAffect, Vector3 atPosition, Vector3 forceToAdd, Space space, ForceMode forceMode)
        {
            GameObject go = gameObjectToAffect;

            Vector3 force = forceToAdd;

            if (space == Space.World)
            {
                go.GetComponent<Rigidbody>().AddForceAtPosition(force, atPosition, forceMode);
            }
            else
            {
                go.GetComponent<Rigidbody>().AddRelativeForce(force, forceMode);
            }
        }
        public static void AddForce(GameObject gameObjectToAffect, Vector3 forceToAdd, Space space, ForceMode forceMode)
        {
            GameObject go = gameObjectToAffect;

            Vector3 force = forceToAdd;

            if (space == Space.World)
            {
                {
                    go.GetComponent<Rigidbody>().AddForce(force, forceMode);
                }
            }
            else
            {
                go.GetComponent<Rigidbody>().AddRelativeForce(force, forceMode);
            }
        }
        #endregion

        #region Get Rotation
        public static void GetRotation(GameObject targetGameobject, out Vector3 toStoreVector, Space space)
        {
            GameObject go = targetGameobject;

            if (space == Space.World)
            {
                Vector3 rotation = go.transform.eulerAngles;
                toStoreVector = rotation;
            }
            else
            {
                Vector3 rotation = go.transform.localEulerAngles;
                toStoreVector = rotation;
            }

        }
        public static void GetRotation(GameObject targetGameobject, out Quaternion toStoreQuaternion, Space space)
        {

            GameObject go = targetGameobject;

            if (space == Space.World)
            {
                toStoreQuaternion = go.transform.rotation;
            }
            else
            {
                Vector3 rotation = go.transform.eulerAngles;
                toStoreQuaternion = Quaternion.Euler(rotation);
            }
        }
        public static void GetRotation(GameObject targetGameobject, XYZ axisToGet, out float toStoreFloat, Space space)
        {

            GameObject go = targetGameobject;

            if (space == Space.World)
            {
                Vector3 rotation = go.transform.eulerAngles;
                switch (axisToGet)
                {

                    case XYZ.X:
                        toStoreFloat = rotation.x;
                        break;
                    case XYZ.Y:
                        toStoreFloat = rotation.y;
                        break;
                    case XYZ.Z:
                        toStoreFloat = rotation.z;
                        break;
                    default:
                        toStoreFloat = 0f;
                        Debug.LogError("No Axis Was Provided!");
                        break;
                }

            }
            else
            {
                Vector3 rotation = go.transform.localEulerAngles;
                switch (axisToGet)
                {
                    case XYZ.X:
                        toStoreFloat = rotation.x;
                        break;
                    case XYZ.Y:
                        toStoreFloat = rotation.y;
                        break;
                    case XYZ.Z:
                        toStoreFloat = rotation.z;
                        break;
                    default:
                        toStoreFloat = 0f;
                        Debug.LogError("No Axis Was Provided!");
                        break;
                }


            }
        }
        #endregion

        #region Set Rotation
        public static Vector3 SetRotation(GameObject toRotate, Vector3 targetRotationVector3, Space space)
        {
            GameObject go = toRotate;

            Vector3 rotation;

            rotation = targetRotationVector3;

            // apply rotation

            if (space == Space.Self)
            {
                go.transform.localEulerAngles = rotation;
                return go.transform.localEulerAngles;
            }
            else
            {
                go.transform.eulerAngles = rotation;
                return go.transform.eulerAngles;
            }
        }
        public static Vector3 SetRotation(GameObject toRotate, Quaternion targetRotationQuaternion, Space space)
        {
            GameObject go = toRotate;

            Vector3 rotation;

            rotation = targetRotationQuaternion.eulerAngles;

            // apply rotation

            if (space == Space.Self)
            {
                go.transform.localEulerAngles = rotation;
                return go.transform.localEulerAngles;
            }
            else
            {
                go.transform.eulerAngles = rotation;
                return go.transform.eulerAngles;
            }
        }
        public static Vector3 SetRotation(GameObject toRotate, XYZ axisToAffect, float rotationAmount, Space space)
        {
            GameObject go = toRotate;

            Vector3 rotation;

            rotation = space == Space.Self ? go.transform.localEulerAngles : go.transform.eulerAngles;
            //Debug.Log("Setting Rotation");
            // apply rotation
            switch (axisToAffect)
            {
                case XYZ.X:
                    rotation.x = rotationAmount;
                    break;
                case XYZ.Y:
                    rotation.y = rotationAmount;
                    break;
                case XYZ.Z:
                    rotation.z = rotationAmount;
                    break;
                default:
                    Debug.LogError("Didn't specify an axis!");
                    break;
            }
            if (space == Space.Self)
            {
                go.transform.localEulerAngles = rotation;
                return go.transform.localEulerAngles;
            }
            else
            {
                go.transform.eulerAngles = rotation;
                return go.transform.eulerAngles;
            }
        }

        #endregion

        #region Get Velocity
        public static void GetVelocity(GameObject gameObjectToGet, out Vector3 toStoreVector3, Space space)
        {

            GameObject go = gameObjectToGet;
            Rigidbody rigidbody = new Rigidbody();
            if (rigidbody != go.GetComponent<Rigidbody>())
                rigidbody = go.GetComponent<Rigidbody>();
            Vector3 velocity = rigidbody.velocity;
            if (space == Space.Self)
            {
                velocity = go.transform.InverseTransformDirection(velocity);
            }
            toStoreVector3 = velocity;
        }
        public static void GetVelocity(GameObject gameObjectToGet, XYZ axis, out float toStoreFloat, Space space)
        {
            GameObject go = gameObjectToGet;
            Rigidbody rigidbody = new Rigidbody();
            if (rigidbody != go.GetComponent<Rigidbody>())
                rigidbody = go.GetComponent<Rigidbody>();
            Vector3 velocity = rigidbody.velocity;
            if (space == Space.Self)
            {
                velocity = go.transform.InverseTransformDirection(velocity);
            }
            switch (axis)
            {
                case XYZ.X:
                    toStoreFloat = velocity.x;
                    break;
                case XYZ.Y:
                    toStoreFloat = velocity.y;
                    break;
                case XYZ.Z:
                    toStoreFloat = velocity.z;
                    break;
                default:
                    toStoreFloat = 0f;
                    Debug.LogError("Didn't specify an axis!");
                    break;
            }


        }
        #endregion

        #region Rotate
        public static void Rotate(GameObject toRotate, Vector3 rotationVector, bool perSecond, Space space)
        {
            //Debug.Log("In Base Rotate!");
            GameObject go = toRotate;

            // Use vector

            Vector3 rotate = rotationVector;

            // apply

            if (!perSecond)
            {
                go.transform.Rotate(rotate, space);
            }
            else
            {
                go.transform.Rotate(rotate * Time.deltaTime, space);
            }
        }
        public static void Rotate(GameObject toRotate, XYZ axisToOverride, float overrideAmount, bool perSecond, Space space)
        {
            // Use vector
            Vector3 rotate = new Vector3();
            switch (axisToOverride)
            {
                case XYZ.X:
                    rotate = new Vector3(overrideAmount, 0, 0);
                    break;
                case XYZ.Y:
                    rotate = new Vector3(0, overrideAmount, 0);
                    break;
                case XYZ.Z:
                    rotate = new Vector3(0, 0, overrideAmount);
                    break;
                default:
                    Debug.LogError("No Axis Supplied!");
                    break;
            }
            Rotate(toRotate, rotate, perSecond, space);

        }
        public static void Rotate(GameObject toRotate, float xAngleOverride, float yAngleOverride, float zAngleOverride, bool perSecond, Space space)
        {
            // Use vector
            Vector3 rotate = new Vector3(xAngleOverride, yAngleOverride, zAngleOverride);
            // apply
            rotate.x = xAngleOverride;
            rotate.y = yAngleOverride;
            rotate.z = zAngleOverride;
            Rotate(toRotate, rotate, perSecond, space);
        }
        #endregion

        #region Create Object
        public static void CreateObject(GameObject gameObjectToCreate, GameObject spawnPoint)
        {
            GameObject go = gameObjectToCreate;
            var spawnPosition = Vector3.zero;
            var spawnRotation = Vector3.zero;


            spawnRotation = spawnPoint.transform.eulerAngles;
            spawnPosition = spawnPoint.transform.position;
            Instantiate(go, spawnPosition, Quaternion.Euler(spawnRotation));
        }
        public static void CreateObject(GameObject gameObjectToCreate, Vector3 vectorPoint)
        {
            GameObject go = gameObjectToCreate;
            var spawnPosition = Vector3.zero;

            spawnPosition = vectorPoint;
            Instantiate(go, spawnPosition, Quaternion.identity);
        }
        public static void CreateObject(GameObject gameObjectToCreate, GameObject spawnPoint, out GameObject storeGameObject)
        {
            GameObject go = gameObjectToCreate;
            var spawnPosition = Vector3.zero;
            var spawnRotation = Vector3.zero;
            GameObject newObject;


            spawnRotation = spawnPoint.transform.eulerAngles;
            spawnPosition = spawnPoint.transform.position;
            newObject = (GameObject)Instantiate(go, spawnPosition, Quaternion.Euler(spawnRotation));
            storeGameObject = newObject;
        }
        public static void CreateObject(GameObject gameObjectToCreate, GameObject spawnPoint, Vector3 positionOffset, Vector3 rotation)
        {
            GameObject go = gameObjectToCreate;
            var spawnPosition = Vector3.zero;
            var spawnRotation = Vector3.zero;

            spawnPosition = spawnPoint.transform.position;
            spawnPosition += positionOffset;
            spawnRotation = rotation;
            Instantiate(go, spawnPosition, Quaternion.Euler(spawnRotation));
        }
        public static void CreateObject(GameObject gameObjectToCreate, GameObject spawnPoint, Vector3 positionOffset, Vector3 rotation, out GameObject storeGameObject)
        {
            GameObject go = gameObjectToCreate;
            var spawnPosition = Vector3.zero;
            var spawnRotation = Vector3.zero;
            GameObject newObject;

            spawnPosition += positionOffset;
            spawnRotation = rotation;
            newObject = (GameObject)Instantiate(go, spawnPosition, Quaternion.Euler(spawnRotation));
            storeGameObject = newObject;
        }
        public static void CreateObject(GameObject gameObjectToCreate, GameObject spawnPoint, AffectType whatToAffect, Vector3 affectValue)
        {
            GameObject go = gameObjectToCreate;
            var spawnPosition = Vector3.zero;
            var spawnRotation = Vector3.zero;
            //GameObject newObject;

            switch (whatToAffect)
            {
                case AffectType.Rotation:
                    spawnRotation = affectValue;
                    spawnPosition = spawnPoint.transform.position;

                    Instantiate(go, spawnPosition, Quaternion.Euler(spawnRotation));
                    break;
                case AffectType.Position:
                    spawnPosition += affectValue;
                    spawnRotation = spawnPoint.transform.eulerAngles;
                    Instantiate(go, spawnPosition, Quaternion.Euler(spawnRotation));
                    break;
                default:
                    break;
            }

        }
        public static void CreateObject(GameObject gameObjectToCreate, GameObject spawnPoint, AffectType whatToAffect, Vector3 affectValue, out GameObject storeGameObject)
        {
            GameObject go = gameObjectToCreate;
            var spawnPosition = Vector3.zero;
            var spawnRotation = Vector3.zero;
            GameObject newObject;

            switch (whatToAffect)
            {
                case AffectType.Rotation:
                    spawnRotation = affectValue;
                    spawnPosition = spawnPoint.transform.position;

                    newObject = (GameObject)Instantiate(go, spawnPosition, Quaternion.Euler(spawnRotation));
                    storeGameObject = newObject;
                    break;
                case AffectType.Position:
                    spawnPosition += affectValue;
                    spawnRotation = spawnPoint.transform.eulerAngles;
                    newObject = (GameObject)Instantiate(go, spawnPosition, Quaternion.Euler(spawnRotation));
                    storeGameObject = newObject;
                    break;
                default:
                    storeGameObject = null;
                    break;
            }

        }
        #endregion

        public static void TransformDirection(GameObject targetGameObject, Vector3 localDirection, out Vector3 storeVector)
        {
            GameObject go = targetGameObject;
            storeVector = go.transform.TransformDirection(localDirection);
        }

        public static void GetPosition(GameObject getPosFromGameObject, out Vector3 storePosition, Space space)
        {
            var go = getPosFromGameObject;
            Vector3 position = space == Space.World ? go.transform.position : go.transform.localPosition;

            storePosition = position;
        }

        public static void RandomizeFloat(float min, float max, out float result)
        {
            result = Random.Range(min, max);
        }

        public static void FloatOperation(float float1, float float2, Operation operation, out float storeResult)
        {
            float f1 = float1;
            float f2 = float2;
            switch (operation)
            {
                case Operation.Add:
                    storeResult = f1 + f2;
                    break;
                case Operation.Subtract:
                    storeResult = f1 - f2;
                    break;
                case Operation.Multiply:
                    storeResult = f1 * f2;
                    break;
                case Operation.Divide:
                    storeResult = f1 / f2;
                    break;
                case Operation.Min:
                    storeResult = Mathf.Min(f1, f2);
                    break;
                case Operation.Max:
                    storeResult = Mathf.Max(f1, f2);
                    break;
                default:
                    storeResult = 0f;
                    Debug.LogError("No Operation was assigned!");
                    break;
            }
        }

        public static void FloatInterpolate(InterpolationType mode, float fromFloat, float toFloat, float time, out float storeResult)
        {
            float currentTime = 0f;
            currentTime += Time.deltaTime;
            float lerpTime = currentTime / time;
            switch (mode)
            {
                case InterpolationType.Linear:
                    storeResult = Mathf.Lerp(fromFloat, toFloat, lerpTime);
                    break;
                case InterpolationType.EaseInOut:
                    storeResult = Mathf.SmoothStep(fromFloat, toFloat, lerpTime);
                    break;
                default:
                    storeResult = 0f;
                    Debug.LogError("No mode was specified!");
                    break;
            }

        }

        public static float ReturnPercentage(float min, float max, float current)
        {
            float percentage = 100 * (current - min) / (max - min);
            return percentage;
        }

        public static bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
            //return ((a < b) ? (b - a) : (a - b)) <= threshold;
        }

        public static Vector3 Vector3ClampMagnitude(Vector3 vectorToClamp, float maxLength)
        {
            vectorToClamp = Vector3.ClampMagnitude(vectorToClamp, maxLength);
            return vectorToClamp;
        }

        public static void SetParent(GameObject gameObject, GameObject parent, bool resetLocalPosition, bool resetLocalRotation)
        {
            GameObject go = gameObject;
            go.transform.parent = parent.transform;

            if (resetLocalPosition)
            {
                go.transform.localPosition = Vector3.zero;
            }

            if (resetLocalRotation)
            {
                go.transform.localRotation = Quaternion.identity;
            }
        }

        public static float GetSpeed(GameObject targetGameObject)
        {
            GameObject go = targetGameObject;
            Vector3 velocity = go.GetComponent<Rigidbody>().velocity;
            return velocity.magnitude;
        }

        public static bool CubeCastBool(Vector3 relativePosition, float x, float y, float z, int layerMask, bool keepInCenter, float yOffSet)
        {
            bool hasHit;
            RaycastHit[] hits = new RaycastHit[12];
            #region Bottom Verts
            Vector3[] bottomVerts = new Vector3[4];
            if (keepInCenter)
                relativePosition -= new Vector3(x / 2, -yOffSet, z / 2);
            bottomVerts[0] = relativePosition;
            bottomVerts[1] = new Vector3(relativePosition.x, relativePosition.y, relativePosition.z + z);
            bottomVerts[2] = new Vector3(relativePosition.x + x, relativePosition.y, relativePosition.z);
            bottomVerts[3] = new Vector3(relativePosition.x + x, relativePosition.y, relativePosition.z + z);

            Physics.Linecast(bottomVerts[0], bottomVerts[1], out hits[0]);
            Physics.Linecast(bottomVerts[0], bottomVerts[2], out hits[1]);
            Physics.Linecast(bottomVerts[1], bottomVerts[3], out hits[2]);
            Physics.Linecast(bottomVerts[2], bottomVerts[3], out hits[3]);
            #endregion

            #region Top Verts
            Vector3[] topVerts = new Vector3[4];

            topVerts[0] = new Vector3(relativePosition.x, relativePosition.y + y, relativePosition.z); ;
            topVerts[1] = new Vector3(relativePosition.x, relativePosition.y + y, relativePosition.z + z);
            topVerts[2] = new Vector3(relativePosition.x + x, relativePosition.y + y, relativePosition.z);
            topVerts[3] = new Vector3(relativePosition.x + x, relativePosition.y + y, relativePosition.z + z);

            Physics.Linecast(topVerts[0], topVerts[1], out hits[4], layerMask);
            Physics.Linecast(topVerts[0], topVerts[2], out hits[5], layerMask);
            Physics.Linecast(topVerts[1], topVerts[3], out hits[6], layerMask);
            Physics.Linecast(topVerts[2], topVerts[3], out hits[7], layerMask);
            #endregion

            #region Sides
            Physics.Linecast(topVerts[0], bottomVerts[0], out hits[8], layerMask);
            Physics.Linecast(topVerts[1], bottomVerts[1], out hits[9], layerMask);
            Physics.Linecast(topVerts[2], bottomVerts[2], out hits[10], layerMask);
            Physics.Linecast(topVerts[3], bottomVerts[3], out hits[11], layerMask);
            #endregion
            bool lineHit = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null)
                {
                    Debug.Log("Line " + i + " has hit " + hits[i].collider);
                    lineHit = true;
                }
                else
                {
                    lineHit = false;
                }
            }
            if (lineHit)
                hasHit = true;
            else hasHit = false;
            return hasHit;
        }

        public static void CubeCastHit(Vector3 relativePosition, float x, float y, float z, int layerMask, bool keepInCenter, float yOffSet, out RaycastHit hitReturn)
        {
            RaycastHit[] hits = new RaycastHit[12];
            //RaycastHit nullRayHit = new RaycastHit();

            #region Bottom Verts
            Vector3[] bottomVerts = new Vector3[4];
            if (keepInCenter)
                relativePosition -= new Vector3(x / 2, -yOffSet, z / 2);
            bottomVerts[0] = relativePosition;
            bottomVerts[1] = new Vector3(relativePosition.x, relativePosition.y, relativePosition.z + z);
            bottomVerts[2] = new Vector3(relativePosition.x + x, relativePosition.y, relativePosition.z);
            bottomVerts[3] = new Vector3(relativePosition.x + x, relativePosition.y, relativePosition.z + z);

            Physics.Linecast(bottomVerts[0], bottomVerts[1], out hits[0]);
            Physics.Linecast(bottomVerts[0], bottomVerts[2], out hits[1]);
            Physics.Linecast(bottomVerts[1], bottomVerts[3], out hits[2]);
            Physics.Linecast(bottomVerts[2], bottomVerts[3], out hits[3]);
            #endregion

            #region Top Verts
            RaycastHit rayHitTop;
            Vector3[] topVerts = new Vector3[4];

            topVerts[0] = new Vector3(relativePosition.x, relativePosition.y + y, relativePosition.z); ;
            topVerts[1] = new Vector3(relativePosition.x, relativePosition.y + y, relativePosition.z + z);
            topVerts[2] = new Vector3(relativePosition.x + x, relativePosition.y + y, relativePosition.z);
            topVerts[3] = new Vector3(relativePosition.x + x, relativePosition.y + y, relativePosition.z + z);

            Physics.Linecast(topVerts[0], topVerts[1], out rayHitTop);
            Physics.Linecast(topVerts[0], topVerts[2], out rayHitTop);
            Physics.Linecast(topVerts[1], topVerts[3], out rayHitTop);
            Physics.Linecast(topVerts[2], topVerts[3], out rayHitTop);

            #endregion

            #region Sides
            Physics.Linecast(topVerts[0], bottomVerts[0], out hits[8]);
            Physics.Linecast(topVerts[1], bottomVerts[1], out hits[9]);
            Physics.Linecast(topVerts[2], bottomVerts[2], out hits[10]);
            Physics.Linecast(topVerts[3], bottomVerts[3], out hits[11]);
            #endregion


            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider != null)
                {
                    Debug.Log("Line " + i + " has hit " + hits[i].collider);
                    //nullRayHit = hits[i];
                }
            }
            hitReturn = rayHitTop;
        }

        public static void CubeCastVisualizer(Vector3 relativePosition, float x, float y, float z, bool keepInCenter, float yOffSet)
        {
            if (keepInCenter)
                relativePosition -= new Vector3(x / 2, -yOffSet, z / 2);
            #region Bottom Verts
            Vector3[] bottomVerts = new Vector3[4];

            bottomVerts[0] = relativePosition;
            bottomVerts[1] = new Vector3(relativePosition.x, relativePosition.y, relativePosition.z + z);
            bottomVerts[2] = new Vector3(relativePosition.x + x, relativePosition.y, relativePosition.z);
            bottomVerts[3] = new Vector3(relativePosition.x + x, relativePosition.y, relativePosition.z + z);

            Gizmos.DrawLine(bottomVerts[0], bottomVerts[1]);
            Gizmos.DrawLine(bottomVerts[0], bottomVerts[2]);
            Gizmos.DrawLine(bottomVerts[1], bottomVerts[3]);
            Gizmos.DrawLine(bottomVerts[2], bottomVerts[3]);
            #endregion

            #region Top Verts
            Vector3[] topVerts = new Vector3[4];

            topVerts[0] = new Vector3(relativePosition.x, relativePosition.y + y, relativePosition.z); ;
            topVerts[1] = new Vector3(relativePosition.x, relativePosition.y + y, relativePosition.z + z);
            topVerts[2] = new Vector3(relativePosition.x + x, relativePosition.y + y, relativePosition.z);
            topVerts[3] = new Vector3(relativePosition.x + x, relativePosition.y + y, relativePosition.z + z);

            Gizmos.DrawLine(topVerts[0], topVerts[1]);
            Gizmos.DrawLine(topVerts[0], topVerts[2]);
            Gizmos.DrawLine(topVerts[1], topVerts[3]);
            Gizmos.DrawLine(topVerts[2], topVerts[3]);
            #endregion

            #region Sides
            Gizmos.DrawLine(topVerts[0], bottomVerts[0]);
            Gizmos.DrawLine(topVerts[1], bottomVerts[1]);
            Gizmos.DrawLine(topVerts[2], bottomVerts[2]);
            Gizmos.DrawLine(topVerts[3], bottomVerts[3]);
            #endregion

        }

        public static void WhateverTheFuckYouWant()
        {
            Debug.Log("Tony can do whatever he wants.");
        }

        public static void MakeGame()
        {
            Debug.Log("Congrats on the game. Well done, now go put it up on Steam Greenlight.");
        }

    }
}
