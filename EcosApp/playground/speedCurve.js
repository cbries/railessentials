class SpeedCurve {
	constructor(options = {}) {
		this.__rootContainerClass = ".container";
		this.__recentMouseMoveCoord = { x: 0, y: 0, topPage: 0, leftPage: 0 };
		this.__maxWidth = 500;
		this.__maxHeight = 200;
		this.__mouseDown = false;
		this.__speedCfg = window.__speedCfg;
	}
	
	install() {
		const self = this;
		document.body.onmousedown = function() { self.__mouseDown = true; }
	  document.body.onmouseup = function() { self.__mouseDown = false; }
	
		window.addEventListener("resize", function(){
			self.__redrawSpeedDots();
		});
	
		self.__initDiagram();		
	}
	
	__initDiagram() {
	  const self = this;
		const rootContainer = $(self.__rootContainerClass);
	
		rootContainer.css({ 
			width: self.__maxWidth + "px", 
			height: self.__maxHeight + "px"
		});
		
		const parentOffsetTop = rootContainer.get(0).getBoundingClientRect().top;
		const parentOffsetLeft = rootContainer.get(0).getBoundingClientRect().left;
		const xsteps_tmp = self.__maxWidth / self.__speedCfg.maxSpeedsteps;
		const xsteps = (self.__maxWidth + xsteps_tmp - (self.__speedCfg.speedNoobyWidth / 2) + self.__speedCfg.extraWidthForSteps ) / self.__speedCfg.maxSpeedsteps;
		let elementsToAppend = $();
		for(let i=0; i < self.__speedCfg.maxSpeedsteps; ++i) {
			const nooby = $("<div>")
				.css({
					width: self.__speedCfg.speedNoobyWidth + "px",
					height: self.__speedCfg.speedNoobyHeight + "px"
				})
				.data("index", i)
				.data("xsteps", xsteps)
				.addClass("nooby_" + i)
				.addClass("nooby");
				elementsToAppend = elementsToAppend.add(nooby);
		}
		rootContainer.append(elementsToAppend);
		self.__redrawSpeedDots();
		
		rootContainer.mouseleave(function(ev) {
			self.__mouseDown = false;
		});
		
		rootContainer.mousemove(function(ev) {
			const coord = self.__getMouseCoordRelativeTo(this, ev);
			self.__recentMouseMoveCoord = coord;
			if(self.__mouseDown === false) return;	
			self.__handleMouseClickMove(coord);
		});
		
		rootContainer.click(function(ev){	
			self.__handleMouseClickMove(self.__recentMouseMoveCoord);
		});
	}
	
	__redrawSpeedDots() {
		const self = this;
		const rootContainer = $(self.__rootContainerClass);
		const parentOffsetTop = rootContainer.get(0).getBoundingClientRect().top;
		const parentOffsetLeft = rootContainer.get(0).getBoundingClientRect().left;
		const elements = $('.nooby');
		for(let i = 0; i < elements.length; ++i) {
			const el = $(elements[i]);
			const idx = el.data("index");
			const xsteps = el.data("xsteps");
			el.css({
				//top: parentOffsetTop + "px",
				left: (parentOffsetLeft + parseInt(i*xsteps) - (self.__speedCfg.speedNoobyWidth / 2-1)) + "px"
			});
		}
	}
	
	__handleMouseClickMove(coord) {
		const self = this;
		const steps = self.__maxWidth / self.__speedCfg.maxSpeedsteps;
		const idx = self.__getIndexByWidth(coord.x, self.__maxWidth, steps);
		if(idx < 0) return;		
		const noobyEl = $('.nooby_' + idx);
		if(typeof noobyEl === "undefined" || noobyEl == null || noobyEl.length === 0) 
			return;		
			
		const rootContainer = $(self.__rootContainerClass);
		const parentOffsetTop = rootContainer.get(0).getBoundingClientRect().top;
		const parentHeight = rootContainer.get(0).getBoundingClientRect().height;
		const maxY = parentOffsetTop + parentHeight - self.__speedCfg.speedNoobyHeight;;
			
		if(coord.topPage > maxY) return;
			
		noobyEl.css({
			top: coord.topPage + "px"
		});
	}
	
	__getMouseCoordRelativeTo(rootElement, ev){
		const self = this;
		const evX = ev.pageX;
		const evY = ev.pageY;		
		const parentOffsetTop = rootElement.getBoundingClientRect().top;
		const parentOffsetLeft = rootElement.getBoundingClientRect().left;	
		return {
			x: evX - parentOffsetLeft,
			y: evY - parentOffsetTop,
			topPage: evY,
			leftPage: evX
		};
	}
	
	__getIndexByWidth(x, width, step) {
		if(x < 0) return -1;
		if(x > width) return -1;
		let idx = 0;
		for(let i = 0; i < width; i += step) {
			const lowX = i;
			const highX = lowX + step;
			if(x >= lowX && x < highX)
				return idx;
			++idx;
		}
		return -1;
	}
}