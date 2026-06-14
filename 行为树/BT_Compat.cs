using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Video;

namespace PP.BehaviorTree {
  [Serializable]
  public class TweenSetting {
    public float Duration = 0.3f;
    public float Delay = 0f;
    public int LoopCycle = 0;
    public LoopType LoopType = LoopType.Restart;
    public Ease EaseType = Ease.OutQuad;

    public float DurationValue => Duration;
  }

  public static class BT_ReflectionUtils {
    public static bool TryInvoke(object target, string methodName, params object[] args) {
      if (target == null || string.IsNullOrWhiteSpace(methodName)) return false;
      Type type = target.GetType();
      MethodInfo method = FindBestMethod(type, methodName, args);
      if (method == null) return false;
      object[] converted = ConvertArgs(method.GetParameters(), args);
      method.Invoke(target, converted);
      return true;
    }

    public static object GetMember(object target, string memberName) {
      if (target == null || string.IsNullOrWhiteSpace(memberName)) return null;
      Type type = target.GetType();
      FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (field != null) return field.GetValue(target);
      PropertyInfo prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      return prop?.GetValue(target);
    }

    public static Component FindComponentWithMethod(Transform root, string methodName) {
      if (root == null || string.IsNullOrWhiteSpace(methodName)) return null;
      Component[] components = root.GetComponents<Component>();
      foreach (Component c in components) {
        if (c == null) continue;
        MethodInfo method = c.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method != null) return c;
      }
      return null;
    }

    public static object FindGameManagerInstance(string typeName) {
      if (string.IsNullOrWhiteSpace(typeName)) return null;
      Type type = FindType(typeName);
      if (type == null) return null;
      PropertyInfo instanceProp = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      if (instanceProp == null) return null;
      return instanceProp.GetValue(null);
    }

    public static Type FindType(string fullName) {
      foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
        Type found = asm.GetType(fullName);
        if (found != null) return found;
      }
      return null;
    }

    public static AudioSource ResolveAudioSource(Component source) {
      if (source == null) return null;
      if (source is AudioSource audioSource) return audioSource;
      return source.GetComponent<AudioSource>();
    }

    public static void TryAddToCameraStack(Camera baseCamera, Camera overlayCamera) {
      if (baseCamera == null || overlayCamera == null) return;
      UniversalAdditionalCameraData data = baseCamera.GetComponent<UniversalAdditionalCameraData>();
      if (data == null) return;
      if (data.cameraStack != null && !data.cameraStack.Contains(overlayCamera)) {
        data.cameraStack.Add(overlayCamera);
      }
    }

    public static void TryRemoveFromCameraStack(Camera baseCamera, Camera overlayCamera) {
      if (baseCamera == null || overlayCamera == null) return;
      UniversalAdditionalCameraData data = baseCamera.GetComponent<UniversalAdditionalCameraData>();
      if (data == null || data.cameraStack == null) return;
      data.cameraStack.Remove(overlayCamera);
    }

    private static MethodInfo FindBestMethod(Type type, string methodName, object[] args) {
      MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      foreach (MethodInfo method in methods) {
        if (!string.Equals(method.Name, methodName, StringComparison.Ordinal)) continue;
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length != args.Length) continue;
        if (IsArgsCompatible(parameters, args)) return method;
      }
      return null;
    }

    private static bool IsArgsCompatible(ParameterInfo[] parameters, object[] args) {
      for (int i = 0; i < parameters.Length; i++) {
        Type paramType = parameters[i].ParameterType;
        object arg = args[i];
        if (arg == null) continue;
        if (paramType.IsInstanceOfType(arg)) continue;
        if (paramType.IsEnum && arg is string) continue;
        if (paramType.IsEnum && arg is int) continue;
        if (paramType == typeof(float) && arg is int) continue;
        if (paramType == typeof(int) && arg is float) continue;
        if (paramType == typeof(VideoPlayer) && arg is Component) continue;
        return false;
      }
      return true;
    }

    private static object[] ConvertArgs(ParameterInfo[] parameters, object[] args) {
      object[] converted = new object[args.Length];
      for (int i = 0; i < args.Length; i++) {
        object arg = args[i];
        Type paramType = parameters[i].ParameterType;
        if (arg == null) {
          converted[i] = null;
          continue;
        }
        if (paramType.IsInstanceOfType(arg)) {
          converted[i] = arg;
          continue;
        }
        if (paramType.IsEnum && arg is string argString) {
          converted[i] = Enum.Parse(paramType, argString);
          continue;
        }
        if (paramType.IsEnum && arg is int argInt) {
          converted[i] = Enum.ToObject(paramType, argInt);
          continue;
        }
        if (paramType == typeof(float) && arg is int intValue) {
          converted[i] = (float)intValue;
          continue;
        }
        if (paramType == typeof(int) && arg is float floatValue) {
          converted[i] = (int)floatValue;
          continue;
        }
        if (paramType == typeof(VideoPlayer) && arg is Component component) {
          converted[i] = component.GetComponent<VideoPlayer>();
          continue;
        }
        converted[i] = arg;
      }
      return converted;
    }
  }

  public static class BT_VectorExtensions {
    public static float RandomBetween(this Vector2 range) {
      return UnityEngine.Random.Range(range.x, range.y);
    }
  }

  [Serializable]
  public class VideoSetting {
    public VideoPlayer Player;
    public VideoClip Clip;
    public bool Loop;
  }

  [Serializable]
  public class VideoPlayerSetting {
    public VideoPlayer Player;
    public VideoClip Clip;
    public bool Loop;
  }
}
