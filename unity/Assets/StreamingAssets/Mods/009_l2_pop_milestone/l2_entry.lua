-- 试 L2：用 Lua 局部状态做「只触发一次」的叙事，仍不改世界。
local _fired_4, _fired_8 = false, false

function after_tick(t)
  local n = l2.ape_count
  if not _fired_4 and n >= 4 then
    _fired_4 = true
    log("【试 Mod】同群个体达到 4，西岸「" .. tostring(l2.settlement_west) .. "」侧仍在一起活动。")
  end
  if not _fired_8 and n >= 8 then
    _fired_8 = true
    log("【试 Mod】同群已扩张到 8+，当前刻=" .. tostring(t) .. "。")
  end
end
