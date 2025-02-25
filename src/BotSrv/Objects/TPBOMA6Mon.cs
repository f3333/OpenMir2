﻿using BotSrv.Player;
using SystemModule;

namespace BotSrv.Objects;

public class TPBOMA6Mon : TCatMon
{
    public TPBOMA6Mon(RobotPlayer robotClient) : base(robotClient)
    {
    }

    public override void Run()
    {
        int prv;
        long m_dwFrameTimetime;
        if (m_nCurrentAction == Messages.SM_WALK || m_nCurrentAction == Messages.SM_BACKSTEP ||
            m_nCurrentAction == Messages.SM_RUN || m_nCurrentAction == Messages.SM_HORSERUN) return;
        m_boMsgMuch = false;
        if (m_MsgList.Count >= BotConst.MSGMUCH) m_boMsgMuch = true;
        RunFrameAction(m_nCurrentFrame - m_nStartFrame);
        prv = m_nCurrentFrame;
        if (m_nCurrentAction != 0)
        {
            if (m_nCurrentFrame < m_nStartFrame || m_nCurrentFrame > m_nEndFrame) m_nCurrentFrame = m_nStartFrame;
            if (m_boMsgMuch)
                m_dwFrameTimetime = HUtil32.Round(m_dwFrameTime * 2 / 3);
            else
                m_dwFrameTimetime = m_dwFrameTime;
            if (MShare.GetTickCount() - m_dwStartTime > m_dwFrameTimetime)
            {
                if (m_nCurrentFrame < m_nEndFrame)
                {
                    m_nCurrentFrame++;
                    m_dwStartTime = MShare.GetTickCount();
                }
                else
                {
                    m_nCurrentAction = 0;
                    m_boUseEffect = false;
                }
            }

            m_nCurrentDefFrame = 0;
            m_dwDefFrameTime = MShare.GetTickCount();
        }
        else
        {
            if (MShare.GetTickCount() - m_dwSmoothMoveTime > 200)
            {
                if (MShare.GetTickCount() - m_dwDefFrameTime > 500)
                {
                    m_dwDefFrameTime = MShare.GetTickCount();
                    m_nCurrentDefFrame++;
                    if (m_nCurrentDefFrame >= m_nDefFrameCount) m_nCurrentDefFrame = 0;
                }

                DefaultMotion();
            }
        }

        if (prv != m_nCurrentFrame) m_dwLoadSurfaceTime = MShare.GetTickCount();
    }
}