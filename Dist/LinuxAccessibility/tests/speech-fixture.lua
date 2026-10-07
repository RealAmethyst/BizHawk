-- The private test service signals when it has an AT-SPI object to query.
local directory = os.getenv("BIZHAWK_TEST_DIRECTORY") .. "/"
while true do
    local ready = io.open(directory .. "speech-ready", "r")
    if ready then ready:close(); break end
    emu.yield()
end
speech.say("first", true)
speech.say("second", false)
speech.output("third", false)
speech.say("reject", false)
speech.stop()
print("speech fixture complete")
while true do emu.yield() end
