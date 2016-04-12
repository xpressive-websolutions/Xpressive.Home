import { Component, ElementRef, AfterViewInit } from "angular2/core";
import { DateTimeComponent } from "../dashboard/datetime.component";
import { PlaybarComponent } from "../dashboard/playbar.component";

declare var jQuery: any;

@Component({
    selector: "xh-dashboard",
    templateUrl: "/app/dashboard/dashboard.component.html",
    directives: [DateTimeComponent, PlaybarComponent]
})
export class DashboardComponent implements AfterViewInit {
    constructor(private el: ElementRef) {
    }

    ngAfterViewInit() {
        jQuery(".btn-blog button").on("click", function () {
            var offsets = this.getBoundingClientRect();
            var dd = jQuery(this).parent("div").find(".dropdown-menu")[0];
            var maxHeight = jQuery(window).height();
            var height = jQuery(dd).actualHeight() * 1.3 + 12;
            var hp = "90%";
            var tp = "5%";

            if (height < 120) {
                height = 120;
            }
            if (height < (maxHeight * 0.9)) {
                var h = height / maxHeight;
                var t = (1 - h) / 2;
                hp = Math.round(height) + "px";
                tp = Math.round(t * 100) + "%";
            }

            dd.style.left = offsets.left + "px";
            dd.style.height = hp;
            dd.style.top = tp;
        });

        jQuery(".dropdown-menu").on("click", "li a", function () {
            var selText = jQuery(this).html();
            jQuery(this).parent("li").siblings().removeClass("active");
            jQuery(this).parents(".btn-group").find(".selection").html(selText);
            jQuery(this).parents("li").addClass("active");
        });
    }
}
