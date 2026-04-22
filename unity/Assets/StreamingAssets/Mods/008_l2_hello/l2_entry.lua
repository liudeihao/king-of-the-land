-- L2: after each sim step. API: mod_id, log(msg), l2.* (read-only, refreshed every tick)
function l2_init()
  log("L2 已加载: " .. tostring(mod_id) .. "，初始随机种子=" .. tostring(l2.seed))
end

function after_tick(t)
  if t > 0 and t % 400 == 0 then
    log("刻 " .. tostring(t) .. " 猿=" .. tostring(l2.ape_count)
      .. " 猎物=" .. tostring(l2.prey_count)
      .. " 西岸=" .. tostring(l2.settlement_west or ""))
  end
end
