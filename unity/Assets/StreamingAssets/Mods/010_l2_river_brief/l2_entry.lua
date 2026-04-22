-- 试 L2：读 l2 只读表，做低频简报（可改成只在旱情时打）
function l2_init()
  log("河况简报 L2 已挂接（" .. tostring(mod_id) .. "）。")
end

function after_tick(t)
  if t < 1 then return end
  if t % 350 ~= 0 then return end
  local dl = l2.water_left
  local dr = l2.water_right
  local s = "西岸水位=" .. string.format("%.2f", dl) .. " 东岸=" .. string.format("%.2f", dr)
  if l2.drought then s = s .. "（旱情）" else s = s .. "（非旱情）" end
  s = s .. " 猎物=" .. tostring(l2.prey_count) .. " 掠食=" .. tostring(l2.predator_count)
  log(s)
end
