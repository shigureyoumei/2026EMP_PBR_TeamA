using UnityEditor;
using UnityEngine;

namespace PBR.Editor
{
    [CustomEditor(typeof(ProjectionRoom))]
    public sealed class ProjectionRoomEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("投影室参数（米）。25 × 15 × 7.7 米为 Large Space 公开论文尺寸预设，尚未现场标定；有效投影边界需向管理员确认。通过下方参数调整，不要缩放或移动本对象。",MessageType.Info);
            Field("roomWidth","房间宽度 Width（左右 / X）");
            Field("roomDepth","房间长度 Depth（前后 / Z）");
            Field("roomHeight","房间高度 Height");
            EditorGUILayout.Space();
            Field("eyeHeight","观察眼高 Eye Height");
            Field("testerOffset","站位偏移 Tester Offset（X / Z）");
            Field("projectionBottom","投影下沿距地高度");
            Field("projectionTopMargin","投影上沿距顶距离");
            Field("showRoomGuides","显示房间与观察点辅助线");
            EditorGUILayout.Space();
            Field("seabedViewHeight","海底虚拟视点高度 Y（米）");
            EditorGUILayout.HelpBox("默认 Y = 0.55 米，约高于当前海床 0.8 米。此参数控制虚拟海底视角；上方的观察眼高仍用于真实投影室标定。四面相机、瞄准原点和遮挡判定会一起更新。",MessageType.None);
            if(serializedObject.ApplyModifiedProperties()) {var room=(ProjectionRoom)target;room.Sanitize();room.Apply();SceneView.RepaintAll();}
            EditorGUILayout.HelpBox("中心站位为 (0, 0)。四面相机根据墙面和观察位置计算透视；房间框线只用于标定，不会遮挡海底。停止 Play 后修改并 Ctrl+S 保存，运行中的临时修改不会保留。",MessageType.None);
        }
        void Field(string name,string label) {EditorGUILayout.PropertyField(serializedObject.FindProperty(name),new GUIContent(label));}
        void OnSceneGUI()
        {
            var room=(ProjectionRoom)target;if(!room.showRoomGuides)return;
            for(int i=0;i<4;i++) {room.Wall(i,out var center,out _,out _,out _);Handles.Label(room.ToVirtual(center),new[]{"Front / 前墙","Right / 右墙","Back / 后墙","Left / 左墙"}[i]);}
            Handles.Label(room.virtualEye,"Tester eye / 观察点");
        }
    }
}
