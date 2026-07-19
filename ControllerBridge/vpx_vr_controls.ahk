; vpx_vr_controls.ahk -- Quest -> VPX controls (no vJoy driver needed)
; Reads Touch controllers via auto_oculus_touch and sends VPX's default keys.
; Place next to auto_oculus_touch.ahk + auto_oculus_touch.dll + vJoyInterface.dll
; (auto_oculus_touch.dll links vJoy, so vJoyInterface.dll must be present to load,
;  even though we never use vJoy). Run with the Oculus runtime active.
;
; Mapping (only active while the VPinballX window is focused):
;   Left trigger        -> Left Shift   (left flipper)
;   Right trigger       -> Right Shift  (right flipper)
;   X button (left)     -> 1            (start game)
;   B button (right)    -> 5            (insert coin)
;   Y button (left)     -> Esc          (exit table -> back to launcher)
;   Right stick back    -> Enter (held) (plunger: pull back, release to launch)
;   Left stick          -> DISABLED     (nudge/tilt removed by request)
; These are VPX defaults. If yours differ, check VPX > Preferences > Configure Keys.

#NoEnv
#SingleInstance Force
#include auto_oculus_touch.ahk
SendMode Input            ; if VPX misses inputs, change to: SendMode Play
OnExit, Cleanup

; VPX executable -- keys are only sent when this window is in the foreground,
; so the controls never leak into the launcher menu or any other app.
vpxExe := "ahk_exe VPinballX_GL64.exe"

; thresholds (tune to taste)
trigPress := 0.7
trigRel   := 0.4
plungPull := -0.5         ; right stick Y past this (pulled back) = charge plunger
plungRel  := -0.3

; held-key states
leftFlip  := false
rightFlip := false
plunger   := false

InitOculus()

Loop
{
    Poll()
    lt := GetAxis(AxisIndexTriggerLeft)
    rt := GetAxis(AxisIndexTriggerRight)
    ry := GetAxis(AxisYRight)

    if WinActive(vpxExe)
    {
        ; --- Flippers (hold while squeezed) ---
        if (!leftFlip && lt > trigPress)
        {
            leftFlip := true
            Send {LShift down}
        }
        else if (leftFlip && lt < trigRel)
        {
            leftFlip := false
            Send {LShift up}
        }

        if (!rightFlip && rt > trigPress)
        {
            rightFlip := true
            Send {RShift down}
        }
        else if (rightFlip && rt < trigRel)
        {
            rightFlip := false
            Send {RShift up}
        }

        ; --- Start / Coin / Exit (one tap per button press) ---
        ; VPX/PinMAME polls the keyboard state, so an instantaneous
        ; down+up from SendInput can fall between polls and be lost
        ; (the start switch especially). Hold each tapped key briefly.
        if IsPressed(ovrX)
        {
            LogEvent("X pressed -> sending 1 (start)")
            TapKey("1")     ; start game (X on left controller)
        }
        if IsPressed(ovrB)
        {
            LogEvent("B pressed -> sending 5 (coin)")
            TapKey("5")     ; insert coin
        }
        if IsPressed(ovrY)
        {
            LogEvent("Y pressed -> sending Esc (exit)")
            TapKey("Esc")   ; exit table
        }

        ; --- Plunger: pull right stick back to charge, release to launch ---
        if (!plunger && ry < plungPull)
        {
            plunger := true
            Send {Enter down}
        }
        else if (plunger && ry > plungRel)
        {
            plunger := false
            Send {Enter up}
        }

        focusState := "YES"
    }
    else
    {
        ; VPX not focused -- forget held states so nothing sticks or leaks.
        leftFlip  := false
        rightFlip := false
        plunger   := false
        focusState := "no (menu/other window)"

        ; diagnostic: a button press that lands here was seen by the bridge
        ; but ignored because VPX was not the foreground window.
        if IsPressed(ovrX)
            LogEvent("X pressed but IGNORED -- VPX window not focused (active: " ActiveWinDesc() ")")
        if IsPressed(ovrB)
            LogEvent("B pressed but IGNORED -- VPX window not focused (active: " ActiveWinDesc() ")")
    }

    ; diagnostic: which buttons are currently held (helps verify A detection)
    btns := ""
    if IsDown(ovrA)
        btns .= "A "
    if IsDown(ovrB)
        btns .= "B "
    if IsDown(ovrX)
        btns .= "X "
    if IsDown(ovrY)
        btns .= "Y "
    if IsDown(ovrRThumb)
        btns .= "RThumb "
    if IsDown(ovrLThumb)
        btns .= "LThumb "
    if (btns = "")
        btns := "(none)"

    ToolTip, % "VPX focused: " focusState "`nL trig: " Round(lt,2) "   R trig: " Round(rt,2) "`nButtons down: " btns "`n(Ctrl+Alt+Q to quit)"
    Sleep 5
}

^!q::ExitApp        ; Ctrl+Alt+Q quits

; Press a key for long enough that VPX/PinMAME's keyboard polling
; is guaranteed to sample it (a few 60 Hz frames).
TapKey(key)
{
    Send {%key% down}
    Sleep 80
    Send {%key% up}
}

; Append a timestamped line to bridge_debug.log next to this script.
LogEvent(msg)
{
    FormatTime, ts,, yyyy-MM-dd HH:mm:ss
    FileAppend, %ts%  %msg%`n, %A_ScriptDir%\bridge_debug.log
}

; Name of the current foreground window, for the not-focused diagnostic.
ActiveWinDesc()
{
    WinGet, exe, ProcessName, A
    WinGetTitle, title, A
    return exe " / " title
}

Cleanup:
    Send {LShift up}
    Send {RShift up}
    Send {Enter up}
    ExitApp
return
