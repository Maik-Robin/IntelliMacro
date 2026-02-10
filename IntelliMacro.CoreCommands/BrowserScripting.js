var IntelliMacroClickNAct = {};

IntelliMacroClickNAct.label = document.createElement("div");
IntelliMacroClickNAct.label.style.position = "absolute";
IntelliMacroClickNAct.label.style.zIndex = "10000";
IntelliMacroClickNAct.label.style.display = "none";
IntelliMacroClickNAct.label.style.backgroundColor = "yellow";
IntelliMacroClickNAct.label.style.border = "1px solid black";

IntelliMacroClickNAct.frame = document.createElement("div");
IntelliMacroClickNAct.frame.style.position = "absolute";
IntelliMacroClickNAct.frame.style.zIndex = "10000";
IntelliMacroClickNAct.frame.style.display = "none";
IntelliMacroClickNAct.frame.style.border = "2px dashed blue";

document.body.appendChild(IntelliMacroClickNAct.frame);
document.body.appendChild(IntelliMacroClickNAct.label);

IntelliMacroClickNAct.clickhandler = function (){ 
	var thiz = IntelliMacroClickNAct.target;
	if (thiz.tagName.toLowerCase() == "a") {
		// links are caught automatically
		return true;
	} else {
		// TODO window.external
		var param = null;
		if ( IntelliMacroClickNAct.isFindable(thiz) == 2) {
			param = thiz.value;
		}
		window.external.RecordFormElementAction(thiz.form.name, thiz.name, param);
		return false;
	}
};

IntelliMacroClickNAct.mouseover = function() {
	if (this.tagName.toLowerCase() == "a") {
		IntelliMacroClickNAct.label.innerHTML="Follow link";
	} else if (this.tagName.toLowerCase() == "input" && (this.type.toLowerCase() == "reset" || this.type.toLowerCase() == "submit" || this.type.toLowerCase() == "button")) {
		IntelliMacroClickNAct.label.innerHTML="Click button";
	} else {
		IntelliMacroClickNAct.label.innerHTML="Focus input field";
	}
	var x = -3, y = -3, elem = this;
	while (elem != null) {
		x += elem.offsetLeft;
		y += elem.offsetTop;
		elem = elem.offsetParent;
	}
	IntelliMacroClickNAct.frame.style.left = x +"px";
	IntelliMacroClickNAct.frame.style.top = y +"px";
	IntelliMacroClickNAct.frame.style.width = (this.offsetWidth+2)+"px";
	IntelliMacroClickNAct.frame.style.height= (this.offsetHeight+2)+"px";
	IntelliMacroClickNAct.label.style.left = x+"px";
	IntelliMacroClickNAct.label.style.top = (y+this.offsetHeight+5) +"px";
	IntelliMacroClickNAct.label.style.display="block";
	IntelliMacroClickNAct.frame.style.display="block";
	IntelliMacroClickNAct.target = this;
}

IntelliMacroClickNAct.mouseout = function() {
	IntelliMacroClickNAct.label.style.display="none";
	IntelliMacroClickNAct.frame.style.display="none";
}

IntelliMacroClickNAct.frame.onmouseover = function() {
	IntelliMacroClickNAct.label.style.display="block";
	IntelliMacroClickNAct.frame.style.display="block";
}

IntelliMacroClickNAct.frame.onmouseout = IntelliMacroClickNAct.mouseout;
IntelliMacroClickNAct.frame.onclick = IntelliMacroClickNAct.clickhandler;

IntelliMacroClickNAct.isFindable = function(element) {
	if (!document.forms || !document.forms[element.form.name])
		return 0;
	var value = document.forms[element.form.name][element.name];
	if(value == element) {
		return 1;
	} else if (!value) {
		return 0;
	}
	if (value.length) {
		var newvalue = null;
		for (var i = 0; i < value.length; i++) {
			if (value[i].value == element.value) {
				newvalue = value[i];
				break;
			}
		}
		if (newvalue == element) {
			return 2;
		}
	}
	return 0;
}

IntelliMacroClickNAct.init = function(elements, activate) {
	for(var i=0; i<elements.length; i++) {
		var element = elements[i];
		if (element.tagName.toLowerCase() == "a" || 
				(element.name != null && element.form != null && element.form.name != null && IntelliMacroClickNAct.isFindable(element))) {
			if (activate) {
				element.old_onmouseover = element.onmouseover;
				element.onmouseover = IntelliMacroClickNAct.mouseover;
				element.old_onmouseout = element.onmouseout;
				element.onmouseout = IntelliMacroClickNAct.mouseout;
			} else {
				element.onmouseover = element.old_onmouseover;
				element.onmouseout = element.old_onmouseout;
			}
		}
	}
}

// methods below are called from C#

function IntelliMacroBrowserScripting_ClickNActActivate(activate) {
	IntelliMacroClickNAct.init(document.getElementsByTagName("input"), activate);
	IntelliMacroClickNAct.init(document.getElementsByTagName("option"), activate);
	IntelliMacroClickNAct.init(document.getElementsByTagName("textarea"), activate);
	IntelliMacroClickNAct.init(document.getElementsByTagName("a"), activate);
}

function IntelliMacroBrowserScripting_FormElementAction(formName, elementName, elementValue) {
	var element = document.forms[formName][elementName];
	if(element.length) {
		for (var i = 0; i < element.length; i++) {
			if (element[i].value == elementValue) {
				element = element[i];
				break;
			}
		}
		element.checked = !element.checked;
		element.focus();
	} else if (elementValue != null) {
		element.value = elementValue;
	} else {
		element.checked = !element.checked;
		element.focus();
	}
}