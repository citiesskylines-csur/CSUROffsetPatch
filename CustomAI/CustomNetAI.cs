﻿using CSUROffsetPatch.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSUROffsetPatch.CustomAI
{
    public class CustomNetAI: NetAI
    {
        public virtual float CustomGetCollisionHalfWidth()
        {
            if (CSUROffset.IsCSUROffset(m_info))
            {
                return (m_info.m_halfWidth - m_info.m_pavementWidth) / 2f;
            }
            return m_info.m_halfWidth;
        }
    }
}
