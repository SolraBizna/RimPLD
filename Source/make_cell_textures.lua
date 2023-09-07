local COLOR_WHITE="#ffffff"
local COLOR_INACTIVE="#7e1618"
local COLOR_ACTIVE="#2dd800"
--local COLOR_DIM="#333333"
local COLOR_DIM="#616c7a"
local COLOR_UNDIM="#b3b3b3"

local SVG_HEADER = [[
<svg xmlns="http://www.w3.org/2000/svg" version="1.1" viewBox="%g %g %g %g">
]]
local SVG_FOOTER = [[
</svg>
]]

local svg_f, svg_path, png_path
local function begin_svg(name, x, y, w, h)
    assert(not svg_f, "unfinished SVG!")
    x = x or 0
    y = y or 0
    w = w or 32
    h = h or 32
    svg_path = "cell_textures_output/"..name..".svg"
    png_path = "../Textures/UI/RimPLD/"..name..".png"
    svg_f = assert(io.open(svg_path, "wb"))
    assert(svg_f:write(SVG_HEADER:format(x,y,w,h)))
end

local function end_svg(double)
    if double == nil then double = true end
    assert(svg_f, "end_svg without begin_svg")
    assert(svg_f:write(SVG_FOOTER))
    svg_f:close()
    svg_f = nil
    print(svg_path.." -> "..png_path)
    -- ImageMagick's SVG support is buggy
    --os.execute("convert -density 96 '"..svg_path.."' '"..png_path.."'")
    -- GIMP's batch support is terrible and it auto crops SVGs
    --gimpcmd[#gimpcmd+1] = [[--batch '(let* ((image (car (file-svg-load RUN-NONINTERACTIVE "]]..svg_path..[[" "" 72 (- 0 32) (- 0 32) 0))) (drawable (car (gimp-image-get-active-layer image)))) (plug-in-autocrop RUN-NONINTERACTIVE image drawable) (gimp-file-save RUN-NONINTERACTIVE image drawable "]]..png_path..[[" "]]..png_path..[[") (gimp-image-delete image))']]
    -- cairosvg to the rescue!
    assert(os.execute("cairosvg -s 1 '"..svg_path.."' -o '"..png_path.."'", "cairosvg failed!"))
    if double then
        assert(os.execute("cairosvg -s 2 '"..svg_path.."' -o '"..png_path:gsub("%.png$","x2.png").."'", "cairosvg failed!"))
    end
end

local function outf(format,...)
    assert(svg_f:write(format:format(...)))
end

local function node(x, y, color)
    outf('<circle cx="%g" cy="%g" r="3" stroke="none" fill="%s"/>\n',
        x, y, color)
end

local function vertbar(offset, cap, color)
    local x, y, w, h
    if offset then x = 23 else x = 15 end
    w = 2
    if cap == "topmost" then
        y,h = 23,9
    elseif cap == "botmost" then
        y,h = 0,5
    else
        y,h = 0,32
    end
    outf('<rect x="%g" y="%g" width="%g" height="%g" fill="%s" stroke="none"/>\n',
        x, y, w, h, color)
end

local function horbar(offset, cap, color)
    local x, y, w, h
    if offset then y = 23 else y = 15 end
    h = 2
    if cap == "leftmost" then
        x,w = 23,9
    elseif cap == "rightmost" then
        x,w = 0,5
    else
        x,w = 0,32
    end
    outf('<rect x="%g" y="%g" width="%g" height="%g" fill="%s" stroke="none"/>\n',
        x, y, w, h, color)
end

local function outpnt(transpose, cmd, x, y)
    if transpose then x,y = y,x end
    outf('%s %g %g ', cmd, x, y)
end

local function endshape()
    outf('Z ')
end

local function diode(transpose, connect, in_color, out_color)
    local start_x, start_y = 4, 8
    local end_x, end_y = 24, 16
    local diode_in_x = 7.5
    local diode_out_x = 17.5
    local diode_left_y = end_y - 3.5
    local diode_right_y = end_y + 3.5
    local X,Y = "x","y"
    if transpose then X,Y = Y,X end
    local W,H = "width","height"
    if transpose then W,H = H,W end
    if connect then
        outf('<rect %s="%g" %s="%g" %s="%g" %s="%g" fill="%s" stroke="none"/>\n', X, start_x-1, Y, start_y-1, W, 2, H, end_y-start_y+2, in_color)
        outf('<rect %s="%g" %s="%g" %s="%g" %s="%g" fill="%s" stroke="none"/>\n', X, start_x-1, Y, end_y-1, W, diode_in_x-start_x+1, H, 2, in_color)
    end
    if transpose then
        outf('<path fill="%s" stroke="%s" stroke-linecap="round" stroke-width="1" d="', connect and COLOR_UNDIM or "none", in_color)
        outpnt(transpose, "M", diode_in_x, diode_left_y)
        outpnt(transpose, "L", diode_out_x, end_y)
        outpnt(transpose, "L", diode_in_x, diode_right_y)
        endshape()
        outf('"/>\n')
        outf('<rect %s="%g" %s="%g" %s="%g" %s="%g" fill="%s" stroke="none"/>\n', X, diode_out_x-0.5, Y, diode_left_y-0.5, W, 2, H, diode_right_y-diode_left_y+1, out_color)
    else
        outf('<path fill="%s" stroke="%s" stroke-linecap="round" stroke-width="1" d="', connect and COLOR_UNDIM or "none", out_color)
        outpnt(transpose, "M", diode_out_x, diode_left_y)
        outpnt(transpose, "L", diode_in_x, end_y)
        outpnt(transpose, "L", diode_out_x, diode_right_y)
        endshape()
        outf('"/>\n')
        outf('<rect %s="%g" %s="%g" %s="%g" %s="%g" fill="%s" stroke="none"/>\n', X, diode_in_x-1.5, Y, diode_left_y-0.5, W, 2, H, diode_right_y-diode_left_y+1, in_color)
    end
    if connect then
        outf('<rect %s="%g" %s="%g" %s="%g" %s="%g" fill="%s" stroke="none"/>\n', X, diode_out_x, Y, end_y-1, W, end_x-diode_out_x, H, 2, out_color)
    end
end

local function big_diode(transpose, in_color, out_color)
    local diode_in_x = 0.5
    local diode_out_x = 31.5
    local diode_left_y = 16 - 9.5
    local diode_right_y = 16 + 9.5
    local X,Y = "x","y"
    if transpose then X,Y = Y,X end
    local W,H = "width","height"
    if transpose then W,H = H,W end
    outf('<path fill="none" stroke="%s" stroke-linecap="round" stroke-width="1" d="', in_color)
    outpnt(transpose, "M", diode_in_x, diode_left_y)
    outpnt(transpose, "L", diode_out_x, 16)
    outpnt(transpose, "L", diode_in_x, diode_right_y)
    endshape()
    outf('"/>\n')
    outf('<rect %s="%g" %s="%g" %s="%g" %s="%g" fill="%s" stroke="none"/>\n', X, diode_out_x-1.5, Y, diode_left_y-0.5, W, 2, H, diode_right_y-diode_left_y+1, out_color)
end

local function split(t,f)
    for k,v in pairs(t) do
        f(k,v)
    end
end


split({
    Off={COLOR_INACTIVE,COLOR_ACTIVE},
    On={COLOR_ACTIVE,COLOR_INACTIVE},
    Dead={COLOR_INACTIVE,COLOR_INACTIVE},
}, function(a,colors)
    local in_color = colors[1]
    local out_color = colors[2]
    begin_svg("Not"..a, 0, 0, 16, 24)
    outf('<path fill="none" stroke="%s" stroke-linecap="round" stroke-width="1" d="', in_color)
    outf('M 1.5 0.5 L 14.5 0.5 L 8 19 Z')
    outf('"/>\n"')
    outf('<circle cx="8" cy="21.5" r="2" fill="none" stroke="%s" stroke-width="1"/>\n', out_color)
    end_svg()
end)

split({
    W=-1,
    [""]=0,
    E=1,
}, function(a, x)
    split({
        S=1,
        [""]=0,
        N=-1,
    }, function(b, y)
        local stem
        if #a + #b == 0 then
            stem = "C"
        else
            stem = a..b
        end
        begin_svg("Hat"..stem, -12, -12, 24, 24)
        outf([[<path stroke-linecap="square" stroke="%s" stroke-width="2" fill="none" d="M -8 -8 L 8 -8 L 8 8 L -8 8 Z"/>\n]], COLOR_DIM)
        --[=[for _,start in ipairs{"-8 0", "8 0", "0 -8", "0 8"} do
            outf([[<path stroke-linecap="square" stroke="%s" stroke-width="1" fill="none" d="M %s L 0 0"/>\n]], COLOR_DIM, start)
        end]=]
        outf([[<path stroke-linecap="square" stroke="%s" stroke-width="2" fill="none" d="M 0 0 L %g %g"/>\n]], COLOR_UNDIM, x*8, y*8)
        outf([[<circle fill="%s" cx="%g" cy="%g" r="4" stroke="none"/>\n]], COLOR_UNDIM, x*8, y*8)
        end_svg()
    end)
end)

begin_svg("Latch", 0, 0, 64, 64)
outf([[<rect x="0.5" y="0.5" width="63" height="63" stroke="%s" stroke-width="1" fill="none"/>]], COLOR_DIM)
--[[ wow ugly letterforms ]]
--[=[
outf([[<path transform="translate(16 14)" fill="none" stroke="%s" stroke-linecap="round" stroke-width="1" d="]], COLOR_DIM)
outf("M -4.5 -2 A 4.5 6.5 0 1 1 4.5 -2 L 4.5 2 A 4.5 5.5 0 1 1 -4.5 2 Z ")
outf("M 1 4 L 4.5 7.5 ")
outf("M -4.5 -10.5 L 4.5 -10.5 ")
outf('"/>\n')
outf([[<rect x="0.5" y="0.5" width="63" height="63" stroke="%s" stroke-width="1" fill="none"/>]], COLOR_DIM)
outf([[<path transform="translate(48 14)" fill="none" stroke="%s" stroke-linecap="round" stroke-width="1" d="]], COLOR_DIM)
outf("M -4.5 -2 A 4.5 6.5 0 1 1 4.5 -2 L 4.5 2 A 4.5 5.5 0 1 1 -4.5 2 Z ")
outf("M 1 4 L 4.5 7.5 ")
outf('"/>\n')
outf([[<rect x="0.5" y="0.5" width="63" height="63" stroke="%s" stroke-width="1" fill="none"/>]], COLOR_DIM)
outf([[<path transform="translate(48 50)" fill="none" stroke="%s" stroke-linecap="round" stroke-width="1" d="]], COLOR_DIM)
outf("M -4.5 8.5 L -4.5 -8.5 L 0 -8.5 A 3.25 4.25 0 0 1 0 0.5 L -4.5 0.5 M 0 0.5 L 4.5 8.5 ")
outf('"/>\n')
outf([[<path transform="translate(16 50)" fill="none" stroke="%s" stroke-linecap="round" stroke-width="1" d="]], COLOR_DIM)
outf("M 4.5 -6.5 Q 4.5 -8.5 0 -8.5 Q -4.5 -8.5 -4.5 -4.5 Q -4.5 0.5 0 0.5 Q 4.5 0.5 4.5 4.5 Q 4.5 8.5 0 8.5 Q -4.5 8.5 -4.5 6.5")
outf('"/>\n')
]=]
outf([[
<style>
text { font: 12pt Helvetica; fill: %s; text-anchor: middle; }
</style>
<path fill="none" stroke="%s" stroke-linecap="round" stroke-width="1.8" d="M 11.5 4.1 L 21 4.1"/>
<text x="16" y="18">Q</text>
<text x="48" y="18">Q</text>
<text x="16" y="58">S</text>
<text x="48" y="58">R</text>
]], COLOR_UNDIM, COLOR_UNDIM)
end_svg()

split({
    Off=COLOR_INACTIVE,
    On=COLOR_ACTIVE,
}, function(a,color)
    split({
        Top=0,
        Bot=16,
    }, function(which, y)
        begin_svg("Switch"..a..which, 0, -8, 32, 32)
        outf('<path fill="none" stroke="%s" stroke-linecap="round" stroke-width="2" d="M 3 %g L 29 8"/>', color, y)
        node(3, 0, COLOR_UNDIM)
        node(29, 8, COLOR_UNDIM)
        node(3, 16, COLOR_UNDIM)
        end_svg()
    end)
end)

-- gates
split({
    Off=COLOR_INACTIVE,
    On=COLOR_ACTIVE,
    Dead=COLOR_INACTIVE,
}, function(a,color)
    if a ~= "Dead" then
        begin_svg("GndPull"..a)
        outf([[
    <defs>
    <linearGradient id="g" x1="0.375" x2="0.875" y1="0" y2="0">
    <stop offset="0%%" stop-color="%s"/>
    <stop offset="100%%" stop-color="%s"/>
    </linearGradient>
    </defs>
    ]], COLOR_INACTIVE, color)
        outf('<path fill="none" stroke="url(#g)" stroke-linecap="round"  stroke-width="2" d="')
        outf('M 1 13 L 1 19 ')
        outf('M 5 9 L 5 23 ')
        outf('M 9 5 L 9 27 ')
        outf('M 9 16 L 12 16 ')
        outf('l 1.5 -3 l 3 6 l 3 -6 l 3 6 l 3 -6 l 3 6 l 1.5 -3 ')
        outf('L 32 16"/>\n')
        end_svg()
    end
    begin_svg("VccPull"..a)
    outf([[
<defs>
<linearGradient id="g" y1="0.375" y2="0.875" x1="0" x2="0">
<stop offset="0%%" stop-color="%s"/>
<stop offset="100%%" stop-color="%s"/>
</linearGradient>
</defs>
]], a == "Dead" and COLOR_INACTIVE or COLOR_ACTIVE, color)
    outf('<path fill="none" stroke="url(#g)" stroke-linecap="round"  stroke-width="2" d="')
    outf('M 16 1 L 16 5 M 14 3 L 18 3')
    outf('M 5 9 L 27 9 ')
    outf('M 16 9 L 16 12 ')
    outf('l -3 1.5 l 6 3 l -6 3 l 6 3 l -6 3 l 6 3 l -3 1.5 ')
    outf('L 16 32"/>\n')
    end_svg()
    begin_svg("Node"..a,-3,-3,6,6)
    node(0,0,color)
    end_svg()
end)

-- andins
split({
    Off=COLOR_INACTIVE,
    On=COLOR_ACTIVE,
}, function(a,in_color)
    split({
        Off=COLOR_INACTIVE,
        On=COLOR_ACTIVE,
    }, function(b,out_color)
        split({
            Make=true,
            Break=false,
        }, function(c,has)
            local in_color, out_color = in_color, out_color
            local stem
            if c=="Break" then
                if a ~= "Off" or b ~= "Off" then return end
                stem="Break"
                in_color, out_color = COLOR_DIM, COLOR_DIM
            else
                stem=a..b..c
            end
            begin_svg("AndInput"..stem)
            if has then
                if hcap ~= "rightmost" then
                    node(4, 8, in_color)
                end
                if vcap ~= "topmost" then
                    node(24, 16, out_color)
                end
                diode(false, true, in_color, out_color)
            else
                diode(false, false, COLOR_DIM, COLOR_DIM)
            end
            end_svg()
        end)
    end)
end)

-- orins
split({
    Off=COLOR_INACTIVE,
    On=COLOR_ACTIVE,
}, function(a,in_color)
    split({
        Off=COLOR_INACTIVE,
        On=COLOR_ACTIVE,
    }, function(b,out_color)
        split({
            Make=true,
            Break=false,
        }, function(c,has)
            local in_color, out_color = in_color, out_color
            local stem
            if c=="Break" then
                if a ~= "Off" or b ~= "Off" then return end
                stem="Break"
                in_color, out_color = COLOR_DIM, COLOR_DIM
            else
                stem=a..b..c
            end
            begin_svg("OrInput"..stem)
            if has then
                if vcap ~= "botmost" then
                    node(8, 4, in_color)
                end
                if hcap ~= "leftmost" then
                    node(16, 24, out_color)
                end
                diode(true, true, in_color, out_color)
            else
                diode(true, false, in_color, out_color)
            end
            end_svg()
        end)
    end)
end)

split({
    Off=COLOR_INACTIVE,
    On=COLOR_ACTIVE,
    Dim=COLOR_DIM,
}, function(a,color)
    begin_svg(a, 0, 0, 1, 1)
    outf([[<rect x="-1" y="-1" width="3" height="3" stroke="none" fill="%s"/>]], color)
    end_svg(false)
end)

