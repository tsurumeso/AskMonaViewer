$(function() {
    $("a.popup").each(function() {
        var $this = $(this);
        var $body = $("body");
        var $tooltip = $("");

        var t_id = $this.attr("href").split("_")[1];
        var r_id = $this.attr("href").split("_")[2];

        $.support.cors = true;
        $.ajax({
            type: "GET",
            url: "http://askmona.org/v1/responses/list",
            data: {
                t_id: t_id,
                from: r_id
            },
            dataType: "jsonp",
            success: function(data) {
                $this.on("mouseenter", function() {
                    var response = data.responses[0].response
                    response = response.replace(/\r?\n/g, "<br>");
                    $tooltip = $([
                        "<span class='tooltip'>",
                        response,
                        "</span>"
                    ].join(""));
                    $body.append($tooltip);

                    var size = {
                        width: $tooltip.outerWidth(),
                        height: $tooltip.outerHeight()
                    };
                    var offset = $this.offset();

                    $tooltip.css({
                        top: offset.top - size.height,
                        left: offset.left
                    });
                }).on("mouseleave", function() {
                    $tooltip.remove()
                });
            }
        });
    });
})
