import React from 'react';
// material-ui
import { Grid, Link, Pagination, Avatar} from '@material-ui/core';
import {DataGrid} from '@material-ui/data-grid';
import MuiTypography from '@material-ui/core/Typography';
import { renderEditInputCell } from '@material-ui/data-grid';


// project imports
import SubCard from './../../ui-component/cards/SubCard';
import MainCard from './../../ui-component/cards/MainCard';
import SecondaryAction from './../../ui-component/cards/CardSecondaryAction';
import { gridSpacing } from './../../store/constant';

import imgA from './../../assets/images/logo.png';


//==============================|| TYPOGRAPHY ||==============================//

const columns = [
    { field : 'image', headerName : 'Image', width : 300
    ,        renderCell: (params) => (
        //params.value
        
        <img src = {`${params.value}`} width="280" height="130"></img>
        //<img src="/static/media/logo.011a3aea.png"></img>
    )
    },
    { field : 'id', headerName : 'DRONE ID', width : 180},
    { field : 'detail', headerName : 'Detail', width : 800}
]

const rows = [
    { image: '/static/media/logo.011a3aea.png', id:1, detail:'Hello, world!'},
    { image:'i', id:3, detail:'test'},
    { image: '/static/media/logo.011a3aea.png', id:8, detail:'Hello, world!2'},
    { image:'i', id:9, detail:'test2'},
    { image: '/static/media/logo.011a3aea.png', id:11, detail:'Hello, world!3'},
    { image:'i', id:23, detail:'test3'},
    { image:'/static/media/profile.664db376.jpg', id:5, detail:'test4'}
]

function ImageFormatter(value) {
    return (
        <div>
            <img src={`url(${value})`} width="100" height="100" align="middle"/>
        </div>
    );
  }

const TestList = () => {
    return (


        <MainCard title="Test List" secondary={<SecondaryAction link="https://next.material-ui.com/system/typography/" />}>
            
            {/* <Grid container spacing={gridSpacing}>
                <Grid item xs={12} sm={6}>
                    <SubCard title="Extra">
                        <Grid container direction="column" spacing={1}>


                            <img src={imgA} width="16" height="16" align="middle"/>
                            <img src={imgA} width="100" height="100" align="middle"/>

                            
                            
                            <Grid item>
                                <MuiTypography variant="button" display="block" gutterBottom>
                                    Drone ID
                                </MuiTypography>
                            </Grid>
                            <Grid item>
                                <MuiTypography variant="caption" display="block" gutterBottom>
                                    caption text
                                </MuiTypography>
                            </Grid>
                            <Grid item>
                                <MuiTypography variant="overline" display="block" gutterBottom>
                                    Details
                                </MuiTypography>
                            </Grid>
                            <Grid item>
                                <MuiTypography
                                    variant="body2"
                                    color="primary"
                                    component={Link}
                                    href="https://linearjun.com"
                                    target="_blank"
                                    display="block"
                                    gutterBottom
                                    underline="hover"
                                >
                                    https://linearjun.com
                                </MuiTypography>
                            </Grid>
                        </Grid>
                    </SubCard>
                </Grid>
            </Grid> */}


            <div style ={{height:800, width:'100%'}}>
                <DataGrid rows={rows} columns={columns} rowHeight={150} pageSize={5} checkboxSelection/>
            </div>

            {/* <Pagination count={10} color="primary" /> */}

        </MainCard>
    );
};

export default TestList;
