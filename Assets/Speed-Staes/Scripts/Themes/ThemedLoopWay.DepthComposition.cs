using UnityEngine;
namespace SpeedStars.Themes {
 public sealed partial class ThemedLoopWay {
  // Five-section phrases vary density without deleting any catalogue entry.
  int DepthPhrasePhase(int index){int offset=(seed%5+5)%5;return (index+offset)%5;}
  int DepthBuildingBudget(int index){return new[]{2,2,1,2,3}[DepthPhrasePhase(index)];}
  float DepthFront(int index,int ordinal,float near,float middle){int phase=DepthPhrasePhase(index);bool close=phase==4||phase==1&&ordinal==0;return close?near:middle-3-(ordinal%2)*6;}
  void DepthScalePlacement(RomanStreetPlacement item,float factor){item.scale*=factor;item.bounds=MeasuredRomanBounds(item.asset,item.scale,item.yaw);}
 }
}
