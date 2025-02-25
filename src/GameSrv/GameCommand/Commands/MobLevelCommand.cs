﻿using GameSrv.Actor;
using GameSrv.Player;
using SystemModule.Enums;

namespace GameSrv.GameCommand.Commands {
    /// <summary>
    /// 显示你屏幕上你近处所有怪与人的详细情况
    /// </summary>
    [Command("MobLevel", "显示你屏幕上你近处所有怪与人的详细情况", 10)]
    public class MobLevelCommand : GameCommand {
        [ExecuteCommand]
        public void Execute(PlayObject playObject) {
            IList<BaseObject> baseObjectList = new List<BaseObject>();
            playObject.Envir.GetRangeBaseObject(playObject.CurrX, playObject.CurrY, 2, true, baseObjectList);
            for (int i = 0; i < baseObjectList.Count; i++) {
                playObject.SysMsg(baseObjectList[i].GetBaseObjectInfo(), MsgColor.Green, MsgType.Hint);
            }
            baseObjectList.Clear();
        }
    }
}