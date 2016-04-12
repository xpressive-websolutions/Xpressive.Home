import {bootstrap}    from "angular2/platform/browser";
import {AppComponent} from "./app.component";

import * as moment from "moment";

//import {enableProdMode} from 'angular2/core';
//enableProdMode();

moment.locale("de-de");
bootstrap(AppComponent);
