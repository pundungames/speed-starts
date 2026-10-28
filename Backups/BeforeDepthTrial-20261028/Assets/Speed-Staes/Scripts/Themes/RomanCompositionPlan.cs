using System;
using UnityEngine;
namespace SpeedStars.Themes {
 public enum RomanAssetLayer { Building, StreetProp, Plant, Skyline, Countryside }
 [Serializable] public sealed class RomanCompositionSlot {
  public string role,asset;public string[] alternatives;
  public RomanAssetLayer layer;public float distance,x,scale=1,yaw=270;
  public bool focal;
 }
 [Serializable] public sealed class RomanPavedCourt {
  public string name;public float distance,length=20,x=-25,width=20;
 }
 [Serializable] public sealed class RomanDistrict {
  public string name;public float length=60;public RomanCompositionSlot[] slots;public RomanPavedCourt[] courts;
 }
 [CreateAssetMenu(menuName="Speed Stars/Roman Composition")]
 public sealed class RomanCompositionPlan:ScriptableObject {
  public RomanDistrict[] districts;
  public float lampSpacing=11.81f;
 }
}
