$(function() {
    $("a.popup").on("mouseenter", function() {
        $this = $(this)
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
                var response = data.responses[0].response
                response = response.replace(/\r?\n/g, "<br>");
                var $tooltip = $('<span class="tooltip">' + response + '</span>');
                $this.append($tooltip);
                var size = {
                    width: $tooltip.outerWidth(),
                    height: $tooltip.outerHeight()
                };
                var offset = $this.offset();
                $tooltip.css({
                    top: offset.top - size.height,
                    left: offset.left
                });
            }
        });
    }).on("mouseleave", function() {
        $(this).find(".tooltip").remove();
    });
})
