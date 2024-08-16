for a in *.png;
magick $a \( +clone -fx 'p{0,0}' \) \
-compose Difference  -composite  \
-modulate 100,0  -alpha off  difference$a;

magick $a difference$a \
-alpha off -compose CopyOpacity -composite \
new$a
;rm difference$a;
convert new$a -fill white -opaque black color-$a;rm new$a;
end



for a in *.png
    set b $(echo $a | sed "s/lettre-//g" | sed "s/\.png//g" )
    if test 1 -eq $(string length $b); if test $(printf '%d' "'$b"  ) -ge 97
    if test 122 -ge $(printf '%d' "'$b"  )
    mv $a "lettre-"$b"tiny".png
    end;   end
    end
    end